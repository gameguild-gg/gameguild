// import { Certification } from '@/components/certification/certification';
// import { useCallback, useEffect, useState } from 'react';
//
// import { HttpClient, HttpResponse, HttpStatusCode } from '@/lib/core/http';
//
// export class GetCertificationListGateway implements LoadCertificationList {
//   constructor(readonly httpClient: HttpClient) {}
//
//   async getAll(): Promise<HttpResponse<Certification[]>> {
//     return await this.httpClient.request({
//       method: 'GET',
//       url: 'http://localhost:3000/certifications',
//     });
//   }
// }
//
// export type LoadCertificationList = {
//   getAll: () => Promise<HttpResponse<Certification[]>>;
// };
//
// export function useCertifications(loadCertificationList: Readonly<LoadCertificationList>) {
//   const [certifications, setCertifications] = useState<Certification[]>([]);
//
//   const getCertifications = useCallback(async () => {
//     const response = await loadCertificationList.getAll();
//
//     // TODO: handle error.
//     if (response.statusCode !== HttpStatusCode.OK) return;
//
//     if (!response.body) return;
//
//     setCertifications(response.body);
//   }, [loadCertificationList]);
//
//   useEffect(() => {
//     getCertifications();
//   }, [getCertifications]);
//
//   return {
//     certifications,
//   };
// }
