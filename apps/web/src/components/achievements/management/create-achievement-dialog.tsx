'use client';

import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { createAchievement } from '@/lib/achievements/achievements.actions';
import { useState } from 'react';

interface CreateAchievementDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess: () => void;
}

export function CreateAchievementDialog({ open, onOpenChange, onSuccess }: CreateAchievementDialogProps) {
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    category: '',
    type: '',
    points: 10,
    iconUrl: '',
    color: '#FFD700',
    isActive: true,
    isSecret: false,
    isRepeatable: false,
    conditions: '',
    displayOrder: 0,
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      setLoading(true);
      
      const response = await createAchievement({
        body: {
          name: formData.name,
          description: formData.description,
          category: formData.category,
          type: formData.type,
          points: formData.points,
          iconUrl: formData.iconUrl || null,
          color: formData.color,
          isActive: formData.isActive,
          isSecret: formData.isSecret,
          isRepeatable: formData.isRepeatable,
          conditions: formData.conditions || null,
          displayOrder: formData.displayOrder,
        },
      });

      if (response.error) {
        throw new Error('Failed to create achievement');
      }

      // Reset form
      setFormData({
        name: '',
        description: '',
        category: '',
        type: '',
        points: 10,
        iconUrl: '',
        color: '#FFD700',
        isActive: true,
        isSecret: false,
        isRepeatable: false,
        conditions: '',
        displayOrder: 0,
      });

      onSuccess();
    } catch (error) {
      console.error('Error creating achievement:', error);
      alert('Failed to create achievement. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Create New Achievement</DialogTitle>
          <DialogDescription>
            Create a new achievement to reward user accomplishments.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="name">Name *</Label>
              <Input
                id="name"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                placeholder="Achievement name"
                required
              />
            </div>
            
            <div className="space-y-2">
              <Label htmlFor="category">Category</Label>
              <Input
                id="category"
                value={formData.category}
                onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                placeholder="e.g., Learning, Social, Gaming"
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="description">Description</Label>
            <Textarea
              id="description"
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              placeholder="Describe what this achievement represents"
              rows={3}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="type">Type</Label>
              <Input
                id="type"
                value={formData.type}
                onChange={(e) => setFormData({ ...formData, type: e.target.value })}
                placeholder="e.g., milestone, progress, social"
              />
            </div>
            
            <div className="space-y-2">
              <Label htmlFor="points">Points</Label>
              <Input
                id="points"
                type="number"
                value={formData.points}
                onChange={(e) => setFormData({ ...formData, points: parseInt(e.target.value) || 0 })}
                placeholder="Points awarded"
                min="0"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="iconUrl">Icon URL</Label>
              <Input
                id="iconUrl"
                value={formData.iconUrl}
                onChange={(e) => setFormData({ ...formData, iconUrl: e.target.value })}
                placeholder="https://example.com/icon.png"
              />
            </div>
            
            <div className="space-y-2">
              <Label htmlFor="color">Color</Label>
              <Input
                id="color"
                type="color"
                value={formData.color}
                onChange={(e) => setFormData({ ...formData, color: e.target.value })}
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="conditions">Conditions (JSON)</Label>
            <Textarea
              id="conditions"
              value={formData.conditions}
              onChange={(e) => setFormData({ ...formData, conditions: e.target.value })}
              placeholder='{"requiredAction": "complete_course", "count": 1}'
              rows={2}
            />
          </div>

          <div className="space-y-4">
            <div className="flex items-center space-x-2">
              <Switch
                id="isActive"
                checked={formData.isActive}
                onCheckedChange={(checked) => setFormData({ ...formData, isActive: checked })}
              />
              <Label htmlFor="isActive">Active</Label>
            </div>
            
            <div className="flex items-center space-x-2">
              <Switch
                id="isSecret"
                checked={formData.isSecret}
                onCheckedChange={(checked) => setFormData({ ...formData, isSecret: checked })}
              />
              <Label htmlFor="isSecret">Secret Achievement</Label>
            </div>
            
            <div className="flex items-center space-x-2">
              <Switch
                id="isRepeatable"
                checked={formData.isRepeatable}
                onCheckedChange={(checked) => setFormData({ ...formData, isRepeatable: checked })}
              />
              <Label htmlFor="isRepeatable">Repeatable</Label>
            </div>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={loading || !formData.name.trim()}>
              {loading ? 'Creating...' : 'Create Achievement'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
