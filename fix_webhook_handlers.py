import re
import sys

# Read the file
with open(
    r"w:\repositories\game-guild\game-guild\apps\api\Source\Modules\Tenants\Handlers\TenantWebhookHandlers.cs",
    "r",
    encoding="utf-8",
) as f:
    content = f.read()

# Fix handler class declarations - replace return types with Result<T>
content = re.sub(
    r"IRequestHandler<(\w+), (TenantWebhook|bool|TenantWebhookDelivery|IEnumerable<TenantWebhook>|PagedResult<TenantWebhookDelivery>)>",
    r"IRequestHandler<\1, Result<\2>>",
    content,
)

# Fix Handle method signatures
content = re.sub(
    r"public async Task<(TenantWebhook|bool|TenantWebhookDelivery|IEnumerable<TenantWebhook>|PagedResult<TenantWebhookDelivery>)> Handle\(",
    r"public async Task<Result<\1>> Handle(",
    content,
)

# Fix return statements to wrap in Result.Success
# For return webhook; patterns
content = re.sub(
    r"return await _repository\.(\w+)\([^)]+\);",
    r"var result = await _repository.\1(\g<0>[:-15]);"
    + "\n        return Result<TenantWebhook>.Success(result);",
    content,
)

# For simple return statements
content = re.sub(
    r"return (webhook|true|false);", r"return Result.Success(\1);", content
)

# Write the updated file
with open(
    r"w:\repositories\game-guild\game-guild\apps\api\Source\Modules\Tenants\Handlers\TenantWebhookHandlers.cs",
    "w",
    encoding="utf-8",
) as f:
    f.write(content)

print("Fixed TenantWebhookHandlers.cs")
