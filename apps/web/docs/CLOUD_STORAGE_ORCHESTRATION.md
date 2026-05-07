# Orquestração do Armazenamento na Nuvem - GGLexical Editor

## Análise da Arquitetura Existente

### Backend (.NET 9 API)
O projeto já possui uma API robusta em .NET 9 com:
- **Arquitetura Modular**: Módulos bem definidos (Projects, Authentication, Users, etc.)
- **Sistema de Projetos**: Já existe um modelo `Project` completo
- **Autenticação JWT**: Sistema de auth implementado
- **Multi-tenancy**: Suporte a tenants
- **Entity Framework**: Com PostgreSQL
- **CQRS Pattern**: Commands/Queries/Handlers
- **GraphQL + REST**: APIs híbridas

### Frontend (Next.js + TypeScript)
- **Sistema de Sync Preparado**: `SyncManager`, `ApiClient`, `SyncQueue`
- **Armazenamento Local**: `EnhancedStorageAdapter` com IndexedDB
- **Sistema de Hash**: Para detecção de mudanças
- **Configuração de Sync**: `syncConfig` centralizados

## Evolução para Arquitetura Dual

> **⚡ NOVA ARQUITETURA DUAL DISPONÍVEL**
> 
> Para uma implementação mais avançada com **múltiplas opções de cloud storage**, 
> consulte: **[📋 DUAL_CLOUD_STORAGE_ARCHITECTURE.md](./DUAL_CLOUD_STORAGE_ARCHITECTURE.md)**
> 
> Esta nova arquitetura oferece:
> - **Google Drive Integration** (pessoal/offline)
> - **GameGuild Server** (corporativo/colaborativo) 
> - **Storage Local** (privacidade máxima)
> - **Conversão dinâmica** entre tipos de storage
> - **Interface unificada** para múltiplos provedores

## Proposta de Implementação

### 1. Extensão do Modelo Project (Backend)

#### 1.1 Novo Modelo: LexicalProject
```csharp
// GameGuild.Modules.Projects.Models.LexicalProject.cs
namespace GameGuild.Modules.Projects;

[Table("LexicalProjects")]
[Index(nameof(UserId))]
[Index(nameof(StorageType))]
[Index(nameof(CreatedAt))]
[Index(nameof(UpdatedAt))]
public sealed class LexicalProject : BaseEntity
{
    /// <summary>
    /// Nome do projeto Lexical
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Dados serializados do editor Lexical (JSON)
    /// </summary>
    [Required]
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Tags do projeto (JSON array)
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Tamanho estimado em bytes
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Hash para detecção de mudanças
    /// </summary>
    [MaxLength(64)]
    public string? Hash { get; set; }

    /// <summary>
    /// Tipo de armazenamento
    /// </summary>
    public LexicalStorageType StorageType { get; set; } = LexicalStorageType.Local;

    /// <summary>
    /// Status de sincronização
    /// </summary>
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Synced;

    /// <summary>
    /// Usuário proprietário
    /// </summary>
    [Required]
    public virtual User User { get; set; } = null!;
    public Guid UserId { get; set; }

    /// <summary>
    /// Tenant (para multi-tenancy)
    /// </summary>
    public virtual Tenant? Tenant { get; set; }
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Versão para controle de concorrência
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public enum LexicalStorageType
{
    Local = 0,   // Apenas local (não sincronizado)
    Cloud = 1    // Sincronizado na nuvem
}

public enum SyncStatus
{
    Synced = 0,     // Sincronizado
    Pending = 1,    // Pendente de sync
    Conflict = 2,   // Conflito detectado
    LocalOnly = 3   // Apenas local
}
```

#### 1.2 DTOs para API
```csharp
// GameGuild.Modules.Projects.Dtos.LexicalProjectDto.cs
public record LexicalProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Data { get; init; } = string.Empty;
    public string[]? Tags { get; init; }
    public int Size { get; init; }
    public string? Hash { get; init; }
    public LexicalStorageType StorageType { get; init; }
    public SyncStatus SyncStatus { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public Guid UserId { get; init; }
    public Guid? TenantId { get; init; }
}

public record LexicalProjectMetadataDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string[]? Tags { get; init; }
    public int Size { get; init; }
    public string? Hash { get; init; }
    public LexicalStorageType StorageType { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateLexicalProjectDto
{
    public string Name { get; init; } = string.Empty;
    public string Data { get; init; } = string.Empty;
    public string[]? Tags { get; init; }
    public LexicalStorageType StorageType { get; init; } = LexicalStorageType.Local;
}

public record UpdateLexicalProjectDto
{
    public string? Name { get; init; }
    public string? Data { get; init; }
    public string[]? Tags { get; init; }
    public LexicalStorageType? StorageType { get; init; }
}
```

### 2. Controllers REST API

#### 2.1 LexicalProjectsController
```csharp
// GameGuild.Modules.Projects.Controllers.LexicalProjectsController.cs
[ApiController]
[Route("api/lexical-projects")]
[Authorize]
public class LexicalProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;

    public LexicalProjectsController(
        IMediator mediator, 
        IUserContext userContext,
        ITenantContext tenantContext)
    {
        _mediator = mediator;
        _userContext = userContext;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Get metadata of all user's projects for sync comparison
    /// </summary>
    [HttpGet("metadata")]
    public async Task<ActionResult<IEnumerable<LexicalProjectMetadataDto>>> GetProjectsMetadata(
        [FromQuery] LexicalStorageType? storageType = null)
    {
        var query = new GetLexicalProjectsMetadataQuery
        {
            UserId = _userContext.UserId,
            TenantId = _tenantContext.TenantId,
            StorageType = storageType
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get all user's projects with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LexicalProjectDto>>> GetProjects(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string[]? tags = null,
        [FromQuery] LexicalStorageType? storageType = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = new GetLexicalProjectsQuery
        {
            UserId = _userContext.UserId,
            TenantId = _tenantContext.TenantId,
            SearchTerm = searchTerm,
            Tags = tags,
            StorageType = storageType,
            Skip = skip,
            Take = take
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific project by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LexicalProjectDto>> GetProject(Guid id)
    {
        var query = new GetLexicalProjectQuery
        {
            Id = id,
            UserId = _userContext.UserId,
            TenantId = _tenantContext.TenantId
        };

        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Get only the hash of a project for sync verification
    /// </summary>
    [HttpGet("{id:guid}/hash")]
    public async Task<ActionResult<string>> GetProjectHash(Guid id)
    {
        var query = new GetLexicalProjectHashQuery
        {
            Id = id,
            UserId = _userContext.UserId,
            TenantId = _tenantContext.TenantId
        };

        var hash = await _mediator.Send(query);
        
        if (hash == null)
            return NotFound();
            
        return Ok(new { hash });
    }

    /// <summary>
    /// Create a new project
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LexicalProjectDto>> CreateProject(
        CreateLexicalProjectDto request)
    {
        var command = new CreateLexicalProjectCommand
        {
            Name = request.Name,
            Data = request.Data,
            Tags = request.Tags,
            StorageType = request.StorageType,
            UserId = _userContext.UserId,
            TenantId = _tenantContext.TenantId
        };

        var result = await _mediator.Send(command);
        
        return CreatedAtAction(
            nameof(GetProject), 
            new { id = result.Id }, 
            result);
    }

    /// <summary>
    /// Update an existing project
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LexicalProjectDto>> UpdateProject(
        Guid id, 
        UpdateLexicalProjectDto request)
    {
        var command = new UpdateLexicalProjectCommand
        {
            Id = id,
            Name = request.Name,
            Data = request.Data,
            Tags = request.Tags,
            StorageType = request.StorageType,
            UserId = _userContext.UserId,
            TenantId = _tenantContext.TenantId
        };

        var result = await _mediator.Send(command);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Delete a project
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteProject(Guid id)
    {
        var command = new DeleteLexicalProjectCommand
        {
            Id = id,
            UserId = _userContext.UserId,
            TenantId = _tenantContext.TenantId
        };

        var success = await _mediator.Send(command);
        
        if (!success)
            return NotFound();
            
        return NoContent();
    }

    /// <summary>
    /// Bulk sync operation for multiple projects
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult> SyncProjects(
        [FromBody] LexicalProjectSyncRequest request)
    {
        var command = new SyncLexicalProjectsCommand
        {
            Projects = request.Projects,
            UserId = _userContext.UserId,
            TenantId = _tenantContext.TenantId
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }
}

public record LexicalProjectSyncRequest
{
    public LexicalProjectMetadataDto[] Projects { get; init; } = Array.Empty<LexicalProjectMetadataDto>();
}
```

### 3. Commands/Queries/Handlers

#### 3.1 Queries
```csharp
// GameGuild.Modules.Projects.Queries.GetLexicalProjectsMetadataQuery.cs
public record GetLexicalProjectsMetadataQuery : IRequest<IEnumerable<LexicalProjectMetadataDto>>
{
    public Guid UserId { get; init; }
    public Guid? TenantId { get; init; }
    public LexicalStorageType? StorageType { get; init; }
}

public class GetLexicalProjectsMetadataHandler : IRequestHandler<GetLexicalProjectsMetadataQuery, IEnumerable<LexicalProjectMetadataDto>>
{
    private readonly ApplicationDbContext _context;

    public GetLexicalProjectsMetadataHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LexicalProjectMetadataDto>> Handle(
        GetLexicalProjectsMetadataQuery request, 
        CancellationToken cancellationToken)
    {
        var query = _context.LexicalProjects
            .Where(p => p.UserId == request.UserId);

        if (request.TenantId.HasValue)
        {
            query = query.Where(p => p.TenantId == request.TenantId);
        }

        if (request.StorageType.HasValue)
        {
            query = query.Where(p => p.StorageType == request.StorageType);
        }

        var projects = await query
            .Select(p => new LexicalProjectMetadataDto
            {
                Id = p.Id,
                Name = p.Name,
                Tags = !string.IsNullOrEmpty(p.Tags) 
                    ? JsonSerializer.Deserialize<string[]>(p.Tags) 
                    : null,
                Size = p.Size,
                Hash = p.Hash,
                StorageType = p.StorageType,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);

        return projects;
    }
}
```

#### 3.2 Commands
```csharp
// GameGuild.Modules.Projects.Commands.CreateLexicalProjectCommand.cs
public record CreateLexicalProjectCommand : IRequest<LexicalProjectDto>
{
    public string Name { get; init; } = string.Empty;
    public string Data { get; init; } = string.Empty;
    public string[]? Tags { get; init; }
    public LexicalStorageType StorageType { get; init; }
    public Guid UserId { get; init; }
    public Guid? TenantId { get; init; }
}

public class CreateLexicalProjectHandler : IRequestHandler<CreateLexicalProjectCommand, LexicalProjectDto>
{
    private readonly ApplicationDbContext _context;
    private readonly IHashService _hashService;

    public CreateLexicalProjectHandler(
        ApplicationDbContext context,
        IHashService hashService)
    {
        _context = context;
        _hashService = hashService;
    }

    public async Task<LexicalProjectDto> Handle(
        CreateLexicalProjectCommand request, 
        CancellationToken cancellationToken)
    {
        var hash = await _hashService.GenerateHashAsync(request.Data);
        var tagsJson = request.Tags != null 
            ? JsonSerializer.Serialize(request.Tags)
            : null;

        var project = new LexicalProject
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Data = request.Data,
            Tags = tagsJson,
            Size = Encoding.UTF8.GetByteCount(request.Data),
            Hash = hash,
            StorageType = request.StorageType,
            SyncStatus = SyncStatus.Synced,
            UserId = request.UserId,
            TenantId = request.TenantId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.LexicalProjects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);

        return new LexicalProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Data = project.Data,
            Tags = request.Tags,
            Size = project.Size,
            Hash = project.Hash,
            StorageType = project.StorageType,
            SyncStatus = project.SyncStatus,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            UserId = project.UserId,
            TenantId = project.TenantId
        };
    }
}
```

### 4. Atualização do Frontend

#### 4.1 Configuração da API
```typescript
// src/components/block-content-editor/lib/api/editor/lexical-api-client.ts
export class LexicalApiClient extends ApiClient {
  private readonly baseEndpoint = '/api/lexical-projects';

  async getProjectsMetadata(storageType?: 'local' | 'cloud'): Promise<ProjectMetadata[]> {
    const params = new URLSearchParams();
    if (storageType) {
      params.append('storageType', storageType);
    }

    const response = await this.fetchWithAuth(
      `${this.baseEndpoint}/metadata?${params.toString()}`
    );

    if (!response.ok) {
      throw new Error(`Failed to fetch projects metadata: ${response.statusText}`);
    }

    return response.json();
  }

  async getProject(id: string): Promise<ProjectData> {
    const response = await this.fetchWithAuth(`${this.baseEndpoint}/${id}`);
    
    if (!response.ok) {
      throw new Error(`Failed to fetch project: ${response.statusText}`);
    }

    return response.json();
  }

  async getProjectHash(id: string): Promise<string> {
    const response = await this.fetchWithAuth(`${this.baseEndpoint}/${id}/hash`);
    
    if (!response.ok) {
      throw new Error(`Failed to fetch project hash: ${response.statusText}`);
    }

    const data = await response.json();
    return data.hash;
  }

  async createProject(project: CreateProjectRequest): Promise<ProjectData> {
    const response = await this.fetchWithAuth(`${this.baseEndpoint}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        name: project.name,
        data: project.data,
        tags: project.tags,
        storageType: project.storageType || 'local'
      }),
    });

    if (!response.ok) {
      throw new Error(`Failed to create project: ${response.statusText}`);
    }

    return response.json();
  }

  async updateProject(id: string, project: UpdateProjectRequest): Promise<ProjectData> {
    const response = await this.fetchWithAuth(`${this.baseEndpoint}/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(project),
    });

    if (!response.ok) {
      throw new Error(`Failed to update project: ${response.statusText}`);
    }

    return response.json();
  }

  async deleteProject(id: string): Promise<void> {
    const response = await this.fetchWithAuth(`${this.baseEndpoint}/${id}`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      throw new Error(`Failed to delete project: ${response.statusText}`);
    }
  }

  async searchProjects(params: SearchProjectsParams): Promise<ProjectData[]> {
    const searchParams = new URLSearchParams();
    
    if (params.searchTerm) searchParams.append('searchTerm', params.searchTerm);
    if (params.tags?.length) params.tags.forEach(tag => searchParams.append('tags', tag));
    if (params.storageType) searchParams.append('storageType', params.storageType);
    if (params.skip) searchParams.append('skip', params.skip.toString());
    if (params.take) searchParams.append('take', params.take.toString());

    const response = await this.fetchWithAuth(
      `${this.baseEndpoint}?${searchParams.toString()}`
    );

    if (!response.ok) {
      throw new Error(`Failed to search projects: ${response.statusText}`);
    }

    return response.json();
  }

  private async fetchWithAuth(url: string, options: RequestInit = {}): Promise<Response> {
    // Get auth token from your auth system
    const token = await this.getAuthToken();
    
    return this.fetchWithRetry(url, {
      ...options,
      headers: {
        ...options.headers,
        'Authorization': `Bearer ${token}`,
      },
    });
  }

  private async getAuthToken(): Promise<string> {
    // Implementation depends on your auth system
    // This should integrate with your existing auth
    return localStorage.getItem('auth_token') || '';
  }
}

interface CreateProjectRequest {
  name: string;
  data: string;
  tags?: string[];
  storageType?: 'local' | 'cloud';
}

interface UpdateProjectRequest {
  name?: string;
  data?: string;
  tags?: string[];
  storageType?: 'local' | 'cloud';
}

interface SearchProjectsParams {
  searchTerm?: string;
  tags?: string[];
  storageType?: 'local' | 'cloud';
  skip?: number;
  take?: number;
}
```

#### 4.2 Atualização do SyncManager
```typescript
// src/components/block-content-editor/lib/sync/editor/enhanced-sync-manager.ts
export class EnhancedSyncManager extends SyncManager {
  private lexicalApiClient: LexicalApiClient;

  constructor() {
    super();
    this.lexicalApiClient = new LexicalApiClient();
  }

  async syncCloudProjects(): Promise<void> {
    if (!syncConfig.isEnabled()) return;

    try {
      this.emit('syncStart');
      
      // 1. Fetch server metadata
      const serverMetadata = await this.lexicalApiClient.getProjectsMetadata('cloud');
      
      // 2. Get local cloud projects
      const localCloudProjects = await this.storageAdapter.getProjectsByStorageType('cloud');
      
      // 3. Compare and sync
      for (const serverProject of serverMetadata) {
        const localProject = localCloudProjects.find(p => p.id === serverProject.id);
        
        if (!localProject) {
          // Download new project
          await this.downloadProject(serverProject.id);
        } else if (localProject.hash !== serverProject.hash) {
          // Update changed project
          await this.downloadProject(serverProject.id);
        }
      }
      
      // 4. Upload local changes
      await this.uploadPendingChanges();
      
      this.emit('syncComplete', { success: true });
    } catch (error) {
      console.error('Sync failed:', error);
      this.emit('syncError', error);
    }
  }

  private async downloadProject(projectId: string): Promise<void> {
    try {
      const projectData = await this.lexicalApiClient.getProject(projectId);
      
      // Save to local storage with cloud type
      await this.storageAdapter.save(
        projectData.id,
        projectData.name,
        projectData.data,
        projectData.tags || [],
        'cloud'
      );
      
      console.log(`Downloaded project: ${projectData.name}`);
    } catch (error) {
      console.error(`Failed to download project ${projectId}:`, error);
    }
  }

  private async uploadPendingChanges(): Promise<void> {
    const pendingProjects = await this.storageAdapter.getProjectsByStorageType('cloud');
    
    for (const project of pendingProjects) {
      if (project.syncStatus === 'pending') {
        try {
          await this.lexicalApiClient.updateProject(project.id, {
            name: project.name,
            data: project.data,
            tags: project.tags,
            storageType: 'cloud'
          });
          
          // Update sync status
          await this.storageAdapter.updateProjectSyncStatus(project.id, 'synced');
          
        } catch (error) {
          console.error(`Failed to upload project ${project.id}:`, error);
        }
      }
    }
  }

  async convertProjectToCloud(projectId: string): Promise<void> {
    const project = await this.storageAdapter.load(projectId);
    if (!project) throw new Error('Project not found');

    try {
      // Upload to server
      const serverProject = await this.lexicalApiClient.createProject({
        name: project.name,
        data: project.data,
        tags: project.tags,
        storageType: 'cloud'
      });

      // Update local storage type
      await this.storageAdapter.updateProjectStorageType(projectId, 'cloud');
      
      console.log(`Converted project to cloud: ${project.name}`);
    } catch (error) {
      console.error(`Failed to convert project to cloud:`, error);
      throw error;
    }
  }

  async convertProjectToLocal(projectId: string): Promise<void> {
    const project = await this.storageAdapter.load(projectId);
    if (!project) throw new Error('Project not found');

    try {
      // Delete from server
      await this.lexicalApiClient.deleteProject(projectId);

      // Update local storage type
      await this.storageAdapter.updateProjectStorageType(projectId, 'local');
      
      console.log(`Converted project to local: ${project.name}`);
    } catch (error) {
      console.error(`Failed to convert project to local:`, error);
      throw error;
    }
  }
}
```

### 5. Migrations e Setup

#### 5.1 Entity Framework Migration
```csharp
// Add to ApplicationDbContext
public DbSet<LexicalProject> LexicalProjects { get; set; }

// Migration file
public partial class AddLexicalProjects : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LexicalProjects",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Data = table.Column<string>(type: "text", nullable: false),
                Tags = table.Column<string>(type: "text", nullable: true),
                Size = table.Column<int>(type: "integer", nullable: false),
                Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                StorageType = table.Column<int>(type: "integer", nullable: false),
                SyncStatus = table.Column<int>(type: "integer", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LexicalProjects", x => x.Id);
                table.ForeignKey(
                    name: "FK_LexicalProjects_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_LexicalProjects_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_LexicalProjects_UserId",
            table: "LexicalProjects",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_LexicalProjects_StorageType",
            table: "LexicalProjects",
            column: "StorageType");

        migrationBuilder.CreateIndex(
            name: "IX_LexicalProjects_CreatedAt",
            table: "LexicalProjects",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_LexicalProjects_UpdatedAt",
            table: "LexicalProjects",
            column: "UpdatedAt");
    }
}
```

### 6. Configuração e Deploy

#### 6.1 Variáveis de Ambiente
```bash
# Backend (.env)
LEXICAL_SYNC_ENABLED=true
LEXICAL_HASH_ALGORITHM=SHA256
LEXICAL_MAX_PROJECT_SIZE=50MB

# Frontend (.env.local)
NEXT_PUBLIC_SYNC_ENABLED=true
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
NEXT_PUBLIC_SYNC_INTERVAL=30000
```

#### 6.2 Dependency Injection (Backend)
```csharp
// Program.cs ou Module registration
services.AddScoped<ILexicalProjectService, LexicalProjectService>();
services.AddScoped<IHashService, Sha256HashService>();
services.AddScoped<GetLexicalProjectsMetadataHandler>();
services.AddScoped<CreateLexicalProjectHandler>();
// ... outros handlers
```

## Resumo da Implementação

### Benefícios da Arquitetura:
1. **Reutilização**: Aproveita a infraestrutura existente (auth, tenants, etc.)
2. **Escalabilidade**: Padrões CQRS, DI, EF Core
3. **Segurança**: Integrada com sistema de auth existente
4. **Performance**: Hashing, indexação, sync otimizado
5. **Multi-tenancy**: Suporte nativo

### Fases de Implementação:
1. **Fase 1**: Modelos, migrations, APIs básicas
2. **Fase 2**: Sistema de sync, conversão local/cloud
3. **Fase 3**: Interface de usuário para gerenciar storage types
4. **Fase 4**: Otimizações, cache, batch sync

### Considerações:
- **Autenticação**: Integrar com sistema auth existente
- **Permissions**: Verificar se usuário pode acessar projeto
- **Tenant isolation**: Garantir isolamento por tenant
- **Rate limiting**: Para operações de sync
- **Monitoring**: Logs e métricas de sync

Esta implementação fornece uma base sólida para sincronização na nuvem mantendo a compatibilidade com o sistema local existente.
