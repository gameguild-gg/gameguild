'use client';

import { useState } from 'react';
import { useTestingRequestContext, useTestingRequestFilters, useTestingRequestSelection } from '@/components/testing-lab/context/testing-requests.context';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Checkbox } from '@/components/ui/checkbox';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { CheckCircle, Clock, Filter, MoreHorizontal, Search, XCircle, Eye, Download, Calendar } from 'lucide-react';
import { TestingRequest } from '@/lib/testing-lab/testing-requests.actions';

export function TestingRequestManagementContent() {
  const { state } = useTestingRequestContext();
  const { filters, setFilter, resetFilters } = useTestingRequestFilters();
  const { selectedRequestIds, selectRequest, deselectRequest, selectAll, clearSelection } = useTestingRequestSelection();

  // UI State
  const [viewMode, setViewMode] = useState<'cards' | 'row' | 'table'>('cards');
  const [selectedRequest, setSelectedRequest] = useState<TestingRequest | null>(null);
  const [isDetailsDialogOpen, setIsDetailsDialogOpen] = useState(false);
  const [isApproveDialogOpen, setIsApproveDialogOpen] = useState(false);
  const [isRejectDialogOpen, setIsRejectDialogOpen] = useState(false);
  const [approvalNotes, setApprovalNotes] = useState('');
  const [rejectionReason, setRejectionReason] = useState('');

  // Filter the requests from context
  const filteredRequests = state.requests.filter((request) => {
    if (filters.search) {
      const searchLower = filters.search.toLowerCase();
      if (!request.title.toLowerCase().includes(searchLower) && !request.gameTitle.toLowerCase().includes(searchLower) && !request.submittedBy.name.toLowerCase().includes(searchLower)) {
        return false;
      }
    }

    if (filters.status && filters.status !== 'all' && request.status !== filters.status) {
      return false;
    }

    if (filters.priority && filters.priority !== 'all' && request.priority !== filters.priority) {
      return false;
    }

    return true;
  });

  const getStatusBadge = (status: TestingRequest['status']) => {
    const variants = {
      pending: { variant: 'secondary' as const, icon: Clock, label: 'Pending' },
      approved: { variant: 'default' as const, icon: CheckCircle, label: 'Approved' },
      rejected: { variant: 'destructive' as const, icon: XCircle, label: 'Rejected' },
      in_testing: { variant: 'outline' as const, icon: Eye, label: 'In Testing' },
    };

    const { variant, icon: Icon, label } = variants[status];
    return (
      <Badge variant={variant} className="gap-1">
        <Icon className="h-3 w-3" />
        {label}
      </Badge>
    );
  };

  const getPriorityBadge = (priority: TestingRequest['priority']) => {
    const variants = {
      low: { variant: 'outline' as const, className: 'border-green-500 text-green-700' },
      medium: { variant: 'outline' as const, className: 'border-yellow-500 text-yellow-700' },
      high: { variant: 'outline' as const, className: 'border-red-500 text-red-700' },
    };

    const { variant, className } = variants[priority];
    return (
      <Badge variant={variant} className={className}>
        {priority.charAt(0).toUpperCase() + priority.slice(1)}
      </Badge>
    );
  };

  const handleApprove = (request: TestingRequest) => {
    setSelectedRequest(request);
    setIsApproveDialogOpen(true);
  };

  const handleReject = (request: TestingRequest) => {
    setSelectedRequest(request);
    setIsRejectDialogOpen(true);
  };

  const handleViewDetails = (request: TestingRequest) => {
    setSelectedRequest(request);
    setIsDetailsDialogOpen(true);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const renderCards = () => (
    <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
      {filteredRequests.map((request) => (
        <Card key={request.id} className="flex flex-col">
          <CardHeader className="flex-none">
            <div className="flex items-start justify-between">
              <div className="flex-1 min-w-0">
                <CardTitle className="text-lg truncate">{request.title}</CardTitle>
                <p className="text-sm text-muted-foreground mt-1">
                  {request.gameTitle} v{request.gameVersion}
                </p>
              </div>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="sm">
                    <MoreHorizontal className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onClick={() => handleViewDetails(request)}>
                    <Eye className="h-4 w-4 mr-2" />
                    View Details
                  </DropdownMenuItem>
                  {request.status === 'pending' && (
                    <>
                      <DropdownMenuItem onClick={() => handleApprove(request)}>
                        <CheckCircle className="h-4 w-4 mr-2" />
                        Approve
                      </DropdownMenuItem>
                      <DropdownMenuItem onClick={() => handleReject(request)}>
                        <XCircle className="h-4 w-4 mr-2" />
                        Reject
                      </DropdownMenuItem>
                    </>
                  )}
                  {request.downloadUrl && (
                    <DropdownMenuItem asChild>
                      <a href={request.downloadUrl} target="_blank" rel="noopener noreferrer">
                        <Download className="h-4 w-4 mr-2" />
                        Download
                      </a>
                    </DropdownMenuItem>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </CardHeader>
          <CardContent className="flex-1 flex flex-col">
            <p className="text-sm text-muted-foreground mb-4">{request.description}</p>

            <div className="flex flex-wrap gap-2 mb-4">
              {getStatusBadge(request.status)}
              {getPriorityBadge(request.priority)}
            </div>

            <div className="flex flex-wrap gap-1 mb-4">
              {request.tags.map((tag) => (
                <Badge key={tag} variant="secondary" className="text-xs">
                  {tag}
                </Badge>
              ))}
            </div>

            <div className="mt-auto space-y-2 text-xs text-muted-foreground">
              <div className="flex items-center gap-1">
                <span>Submitted by: {request.submittedBy.name}</span>
              </div>
              <div className="flex items-center gap-1">
                <Calendar className="h-3 w-3" />
                <span>{formatDate(request.submittedAt)}</span>
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );

  const renderTable = () => (
    <div className="border rounded-lg">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead className="w-12">
              <Checkbox
                checked={selectedRequestIds.length === filteredRequests.length && filteredRequests.length > 0}
                onCheckedChange={(checked) => {
                  if (checked) {
                    selectAll();
                  } else {
                    clearSelection();
                  }
                }}
              />
            </TableHead>
            <TableHead>Game / Version</TableHead>
            <TableHead>Request Title</TableHead>
            <TableHead>Submitted By</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Priority</TableHead>
            <TableHead>Submitted</TableHead>
            <TableHead className="w-12"></TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {filteredRequests.map((request) => (
            <TableRow key={request.id}>
              <TableCell>
                <Checkbox
                  checked={selectedRequestIds.includes(request.id)}
                  onCheckedChange={(checked) => {
                    if (checked) {
                      selectRequest(request.id);
                    } else {
                      deselectRequest(request.id);
                    }
                  }}
                />
              </TableCell>
              <TableCell>
                <div>
                  <div className="font-medium">{request.gameTitle}</div>
                  <div className="text-sm text-muted-foreground">v{request.gameVersion}</div>
                </div>
              </TableCell>
              <TableCell>
                <div className="max-w-xs truncate">{request.title}</div>
              </TableCell>
              <TableCell>{request.submittedBy.name}</TableCell>
              <TableCell>{getStatusBadge(request.status)}</TableCell>
              <TableCell>{getPriorityBadge(request.priority)}</TableCell>
              <TableCell className="text-sm">{formatDate(request.submittedAt)}</TableCell>
              <TableCell>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="sm">
                      <MoreHorizontal className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem onClick={() => handleViewDetails(request)}>
                      <Eye className="h-4 w-4 mr-2" />
                      View Details
                    </DropdownMenuItem>
                    {request.status === 'pending' && (
                      <>
                        <DropdownMenuItem onClick={() => handleApprove(request)}>
                          <CheckCircle className="h-4 w-4 mr-2" />
                          Approve
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => handleReject(request)}>
                          <XCircle className="h-4 w-4 mr-2" />
                          Reject
                        </DropdownMenuItem>
                      </>
                    )}
                    {request.downloadUrl && (
                      <DropdownMenuItem asChild>
                        <a href={request.downloadUrl} target="_blank" rel="noopener noreferrer">
                          <Download className="h-4 w-4 mr-2" />
                          Download
                        </a>
                      </DropdownMenuItem>
                    )}
                  </DropdownMenuContent>
                </DropdownMenu>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );

  return (
    <div className="space-y-6">
      {/* Filters and Controls */}
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex flex-1 gap-4">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input placeholder="Search requests..." value={filters.search} onChange={(e) => setFilter('search', e.target.value)} className="pl-9" />
          </div>

          <Select value={filters.status} onValueChange={(value) => setFilter('status', value)}>
            <SelectTrigger className="w-40">
              <SelectValue placeholder="Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Status</SelectItem>
              <SelectItem value="pending">Pending</SelectItem>
              <SelectItem value="approved">Approved</SelectItem>
              <SelectItem value="rejected">Rejected</SelectItem>
              <SelectItem value="in_testing">In Testing</SelectItem>
            </SelectContent>
          </Select>

          <Select value={filters.priority} onValueChange={(value) => setFilter('priority', value)}>
            <SelectTrigger className="w-40">
              <SelectValue placeholder="Priority" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Priority</SelectItem>
              <SelectItem value="high">High</SelectItem>
              <SelectItem value="medium">Medium</SelectItem>
              <SelectItem value="low">Low</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={resetFilters}>
            <Filter className="h-4 w-4 mr-2" />
            Clear Filters
          </Button>

          <Select value={viewMode} onValueChange={(value: 'cards' | 'row' | 'table') => setViewMode(value)}>
            <SelectTrigger className="w-32">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="cards">Cards</SelectItem>
              <SelectItem value="row">Row</SelectItem>
              <SelectItem value="table">Table</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Content */}
      <div className="space-y-4">
        {filteredRequests.length === 0 ? (
          <div className="text-center py-12">
            <div className="text-muted-foreground">No testing requests found.</div>
          </div>
        ) : (
          <>
            {viewMode === 'cards' && renderCards()}
            {viewMode === 'table' && renderTable()}
          </>
        )}
      </div>

      {/* Details Dialog */}
      <Dialog open={isDetailsDialogOpen} onOpenChange={setIsDetailsDialogOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>{selectedRequest?.title}</DialogTitle>
            <DialogDescription>
              {selectedRequest?.gameTitle} v{selectedRequest?.gameVersion}
            </DialogDescription>
          </DialogHeader>
          {selectedRequest && (
            <div className="space-y-4">
              <div>
                <Label className="text-sm font-medium">Description</Label>
                <p className="text-sm text-muted-foreground mt-1">{selectedRequest.description}</p>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label className="text-sm font-medium">Status</Label>
                  <div className="mt-1">{getStatusBadge(selectedRequest.status)}</div>
                </div>
                <div>
                  <Label className="text-sm font-medium">Priority</Label>
                  <div className="mt-1">{getPriorityBadge(selectedRequest.priority)}</div>
                </div>
              </div>

              <div>
                <Label className="text-sm font-medium">Submitted By</Label>
                <p className="text-sm text-muted-foreground mt-1">
                  {selectedRequest.submittedBy.name} ({selectedRequest.submittedBy.email})
                </p>
              </div>

              <div>
                <Label className="text-sm font-medium">Submitted At</Label>
                <p className="text-sm text-muted-foreground mt-1">{formatDate(selectedRequest.submittedAt)}</p>
              </div>

              {selectedRequest.reviewedAt && (
                <div>
                  <Label className="text-sm font-medium">Reviewed At</Label>
                  <p className="text-sm text-muted-foreground mt-1">{formatDate(selectedRequest.reviewedAt)}</p>
                  {selectedRequest.reviewedBy && <p className="text-sm text-muted-foreground">by {selectedRequest.reviewedBy.name}</p>}
                </div>
              )}

              {selectedRequest.testingPeriod && (
                <div>
                  <Label className="text-sm font-medium">Testing Period</Label>
                  <p className="text-sm text-muted-foreground mt-1">
                    {formatDate(selectedRequest.testingPeriod.startDate)} - {formatDate(selectedRequest.testingPeriod.endDate)}
                  </p>
                </div>
              )}

              {selectedRequest.instructions && (
                <div>
                  <Label className="text-sm font-medium">Instructions</Label>
                  <p className="text-sm text-muted-foreground mt-1">{selectedRequest.instructions}</p>
                </div>
              )}

              <div>
                <Label className="text-sm font-medium">Tags</Label>
                <div className="flex flex-wrap gap-1 mt-1">
                  {selectedRequest.tags.map((tag) => (
                    <Badge key={tag} variant="secondary" className="text-xs">
                      {tag}
                    </Badge>
                  ))}
                </div>
              </div>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsDetailsDialogOpen(false)}>
              Close
            </Button>
            {selectedRequest?.downloadUrl && (
              <Button asChild>
                <a href={selectedRequest.downloadUrl} target="_blank" rel="noopener noreferrer">
                  <Download className="h-4 w-4 mr-2" />
                  Download
                </a>
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Approve Dialog */}
      <Dialog open={isApproveDialogOpen} onOpenChange={setIsApproveDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Approve Testing Request</DialogTitle>
            <DialogDescription>Approve &ldquo;{selectedRequest?.title}&rdquo; for testing.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="approval-notes">Approval Notes (Optional)</Label>
              <Textarea id="approval-notes" placeholder="Add any notes or instructions for the testing session..." value={approvalNotes} onChange={(e) => setApprovalNotes(e.target.value)} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsApproveDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={() => {
                // Here you would call the approve action
                setIsApproveDialogOpen(false);
                setApprovalNotes('');
              }}
            >
              <CheckCircle className="h-4 w-4 mr-2" />
              Approve
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Reject Dialog */}
      <Dialog open={isRejectDialogOpen} onOpenChange={setIsRejectDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reject Testing Request</DialogTitle>
            <DialogDescription>Reject &ldquo;{selectedRequest?.title}&rdquo; with a reason.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="rejection-reason">Rejection Reason *</Label>
              <Textarea id="rejection-reason" placeholder="Please provide a reason for rejection..." value={rejectionReason} onChange={(e) => setRejectionReason(e.target.value)} required />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsRejectDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                // Here you would call the reject action
                setIsRejectDialogOpen(false);
                setRejectionReason('');
              }}
              disabled={!rejectionReason.trim()}
            >
              <XCircle className="h-4 w-4 mr-2" />
              Reject
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
