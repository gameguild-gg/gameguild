// import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
//
// interface SystemStatus {
//   apiStatus: 'online' | 'offline' | 'degraded';
//   databaseStatus: 'connected' | 'disconnected' | 'limited';
//   paymentGatewayStatus: 'active' | 'inactive' | 'limited';
//   emailServiceStatus: 'active' | 'inactive' | 'limited';
// }
//
// interface SystemStatusProps {
//   systemStatus: SystemStatus;
//   className?: string;
// }
//
// export function SystemStatus({ systemStatus, className }: SystemStatusProps) {
//   const getStatusColor = (status: string) => {
//     switch (status) {
//       case 'online':
//       case 'connected':
//       case 'active':
//         return 'bg-green-500';
//       case 'limited':
//       case 'degraded':
//         return 'bg-yellow-500';
//       case 'offline':
//       case 'disconnected':
//       case 'inactive':
//         return 'bg-red-500';
//       default:
//         return 'bg-gray-500';
//     }
//   };
//
//   const getStatusLabel = (status: string) => {
//     switch (status) {
//       case 'online':
//         return 'Online';
//       case 'connected':
//         return 'Connected';
//       case 'active':
//         return 'Active';
//       case 'limited':
//         return 'Limited';
//       case 'degraded':
//         return 'Degraded';
//       case 'offline':
//         return 'Offline';
//       case 'disconnected':
//         return 'Disconnected';
//       case 'inactive':
//         return 'Inactive';
//       default:
//         return 'Unknown';
//     }
//   };
//
//   const getStatusTextColor = (status: string) => {
//     switch (status) {
//       case 'online':
//       case 'connected':
//       case 'active':
//         return 'text-green-400';
//       case 'limited':
//       case 'degraded':
//         return 'text-yellow-400';
//       case 'offline':
//       case 'disconnected':
//       case 'inactive':
//         return 'text-red-400';
//       default:
//         return 'text-gray-400';
//     }
//   };
//
//   const services = [
//     { name: 'API Status', status: systemStatus.apiStatus },
//     { name: 'Database', status: systemStatus.databaseStatus },
//     { name: 'Payment Gateway', status: systemStatus.paymentGatewayStatus },
//     { name: 'Email Service', status: systemStatus.emailServiceStatus },
//   ];
//
//   return (
//     <Card className={`bg-slate-800/50 border-slate-700/50 backdrop-blur-sm ${className}`}>
//       <CardHeader>
//         <CardTitle className="text-base text-white">System Status</CardTitle>
//       </CardHeader>
//       <CardContent className="space-y-3">
//         {services.map((service) => (
//           <div key={service.name} className="flex items-center justify-between">
//             <span className="text-sm text-slate-300">{service.name}</span>
//             <div className="flex items-center gap-2">
//               <div className={`h-2 w-2 rounded-full ${getStatusColor(service.status)}`} />
//               <span className={`text-sm ${getStatusTextColor(service.status)}`}>{getStatusLabel(service.status)}</span>
//             </div>
//           </div>
//         ))}
//       </CardContent>
//     </Card>
//   );
// }
