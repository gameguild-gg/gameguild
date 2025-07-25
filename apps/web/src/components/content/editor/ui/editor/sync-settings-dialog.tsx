'use client';

import { useEffect, useState } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { syncConfig, type SyncConfig } from '@/lib/sync/sync-config';
import { toast } from 'sonner';
import { Bug, Clock, Package, Repeat, Server, Settings, Wifi, WifiOff } from 'lucide-react';

interface SyncSettingsDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function SyncSettingsDialog({ open, onOpenChange }: SyncSettingsDialogProps) {
  const [config, setConfig] = useState<SyncConfig>(syncConfig.getConfig());
  const [tempConfig, setTempConfig] = useState<SyncConfig>(config);
  const [hasChanges, setHasChanges] = useState(false);

  useEffect(() => {
    const unsubscribe = syncConfig.onConfigChange((newConfig) => {
      setConfig(newConfig);
      if (!hasChanges) {
        setTempConfig(newConfig);
      }
    });

    return unsubscribe;
  }, [hasChanges]);

  useEffect(() => {
    const configChanged = JSON.stringify(config) !== JSON.stringify(tempConfig);
    setHasChanges(configChanged);
  }, [config, tempConfig]);

  const handleSave = () => {
    const validation = syncConfig.validateConfig();

    if (!validation.isValid) {
      toast.error('Configuração inválida', {
        description: validation.errors.join(', '),
        duration: 4000,
        icon: '❌',
      });
      return;
    }

    syncConfig.updateConfig(tempConfig);
    setHasChanges(false);

    toast.success('Configurações salvas', {
      description: 'As configurações de sincronização foram atualizadas',
      duration: 3000,
      icon: '✅',
    });
  };

  const handleReset = () => {
    setTempConfig(config);
    setHasChanges(false);
  };

  const handlePreset = (preset: 'development' | 'production' | 'offline') => {
    const presetConfigs = {
      development: {
        enabled: true,
        serverUrl: 'http://localhost:3001/api',
        debugMode: true,
        syncInterval: 10000,
        timeout: 5000,
      },
      production: {
        enabled: true,
        serverUrl: process.env.NEXT_PUBLIC_API_URL || 'https://api.gameguild.dev/api',
        debugMode: false,
        syncInterval: 60000,
        timeout: 15000,
      },
      offline: {
        enabled: false,
        autoSync: false,
      },
    };

    setTempConfig({ ...tempConfig, ...presetConfigs[preset] });
  };

  const getStatusBadge = () => {
    const status = syncConfig.getStatus();
    const variants = {
      enabled: { variant: 'default' as const, icon: <Wifi className="w-3 h-3" />, text: 'Ativo' },
      disabled: { variant: 'secondary' as const, icon: <WifiOff className="w-3 h-3" />, text: 'Desabilitado' },
      offline: { variant: 'destructive' as const, icon: <WifiOff className="w-3 h-3" />, text: 'Offline' },
    };

    const statusConfig = variants[status] || variants.disabled;

    return (
      <Badge variant={statusConfig.variant} className="flex items-center gap-1">
        {statusConfig.icon}
        {statusConfig.text}
      </Badge>
    );
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center justify-between">
            <DialogTitle className="flex items-center gap-2">
              <Settings className="w-5 h-5" />
              Configurações de Sincronização
            </DialogTitle>
            {getStatusBadge()}
          </div>
        </DialogHeader>

        <div className="space-y-6">
          {/* Quick Presets */}
          <div className="space-y-3">
            <Label className="text-sm font-medium">Configurações Rápidas</Label>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={() => handlePreset('development')} className="flex items-center gap-2">
                <Bug className="w-4 h-4" />
                Desenvolvimento
              </Button>
              <Button variant="outline" size="sm" onClick={() => handlePreset('production')} className="flex items-center gap-2">
                <Server className="w-4 h-4" />
                Produção
              </Button>
              <Button variant="outline" size="sm" onClick={() => handlePreset('offline')} className="flex items-center gap-2">
                <WifiOff className="w-4 h-4" />
                Offline
              </Button>
            </div>
          </div>

          <Separator />

          {/* Main Settings */}
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <div className="space-y-1">
                <Label htmlFor="sync-enabled">Habilitar Sincronização</Label>
                <p className="text-xs text-muted-foreground">Ativa a sincronização automática com o servidor</p>
              </div>
              <Switch id="sync-enabled" checked={tempConfig.enabled} onCheckedChange={(enabled) => setTempConfig({ ...tempConfig, enabled })} />
            </div>

            {tempConfig.enabled && (
              <>
                <div className="space-y-2">
                  <Label htmlFor="server-url" className="flex items-center gap-2">
                    <Server className="w-4 h-4" />
                    URL do Servidor
                  </Label>
                  <Input
                    id="server-url"
                    value={tempConfig.serverUrl}
                    onChange={(e) => setTempConfig({ ...tempConfig, serverUrl: e.target.value })}
                    placeholder="http://localhost:3001/api"
                  />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="timeout" className="flex items-center gap-2">
                      <Clock className="w-4 h-4" />
                      Timeout (ms)
                    </Label>
                    <Input
                      id="timeout"
                      type="number"
                      value={tempConfig.timeout}
                      onChange={(e) => setTempConfig({ ...tempConfig, timeout: Number(e.target.value) })}
                      min="1000"
                      max="60000"
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="retries" className="flex items-center gap-2">
                      <Repeat className="w-4 h-4" />
                      Tentativas
                    </Label>
                    <Input
                      id="retries"
                      type="number"
                      value={tempConfig.retries}
                      onChange={(e) => setTempConfig({ ...tempConfig, retries: Number(e.target.value) })}
                      min="0"
                      max="10"
                    />
                  </div>
                </div>

                <div className="flex items-center justify-between">
                  <div className="space-y-1">
                    <Label htmlFor="auto-sync">Sincronização Automática</Label>
                    <p className="text-xs text-muted-foreground">Sincroniza automaticamente em intervalos regulares</p>
                  </div>
                  <Switch id="auto-sync" checked={tempConfig.autoSync} onCheckedChange={(autoSync) => setTempConfig({ ...tempConfig, autoSync })} />
                </div>

                {tempConfig.autoSync && (
                  <div className="space-y-2">
                    <Label htmlFor="sync-interval">Intervalo de Sincronização (ms)</Label>
                    <Input
                      id="sync-interval"
                      type="number"
                      value={tempConfig.syncInterval}
                      onChange={(e) => setTempConfig({ ...tempConfig, syncInterval: Number(e.target.value) })}
                      min="5000"
                      max="300000"
                    />
                    <p className="text-xs text-muted-foreground">Atual: {(tempConfig.syncInterval / 1000).toFixed(0)} segundos</p>
                  </div>
                )}

                <div className="space-y-2">
                  <Label htmlFor="batch-size" className="flex items-center gap-2">
                    <Package className="w-4 h-4" />
                    Tamanho do Lote
                  </Label>
                  <Input
                    id="batch-size"
                    type="number"
                    value={tempConfig.batchSize}
                    onChange={(e) => setTempConfig({ ...tempConfig, batchSize: Number(e.target.value) })}
                    min="1"
                    max="100"
                  />
                  <p className="text-xs text-muted-foreground">Número de projetos processados por vez</p>
                </div>

                <div className="flex items-center justify-between">
                  <div className="space-y-1">
                    <Label htmlFor="debug-mode" className="flex items-center gap-2">
                      <Bug className="w-4 h-4" />
                      Modo Debug
                    </Label>
                    <p className="text-xs text-muted-foreground">Exibe logs detalhados no console</p>
                  </div>
                  <Switch id="debug-mode" checked={tempConfig.debugMode} onCheckedChange={(debugMode) => setTempConfig({ ...tempConfig, debugMode })} />
                </div>
              </>
            )}
          </div>

          <Separator />

          {/* Actions */}
          <div className="flex justify-between">
            <div className="flex gap-2">
              <Button variant="outline" onClick={() => syncConfig.logConfig()} disabled={!tempConfig.debugMode}>
                Ver Config no Console
              </Button>
            </div>

            <div className="flex gap-2">
              <Button variant="outline" onClick={handleReset} disabled={!hasChanges}>
                Cancelar
              </Button>
              <Button onClick={handleSave} disabled={!hasChanges}>
                Salvar Configurações
              </Button>
            </div>
          </div>

          {/* Status Info */}
          {tempConfig.enabled && (
            <div className="p-3 bg-muted rounded-lg space-y-2">
              <h4 className="text-sm font-medium">Informações do Sistema</h4>
              <div className="grid grid-cols-2 gap-2 text-xs">
                <div>
                  <span className="text-muted-foreground">Status da Rede:</span>
                  <span className="ml-2">{navigator.onLine ? 'Online' : 'Offline'}</span>
                </div>
                <div>
                  <span className="text-muted-foreground">Sincronização:</span>
                  <span className="ml-2">{tempConfig.enabled ? 'Habilitada' : 'Desabilitada'}</span>
                </div>
              </div>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
