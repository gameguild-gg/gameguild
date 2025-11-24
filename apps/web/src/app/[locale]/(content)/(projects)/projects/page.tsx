import React from 'react';
import {getQueryClient} from "@/components/get-query-client";
import {dehydrate, HydrationBoundary} from "@tanstack/react-query";

export default async function Page(): Promise<React.JSX.Element> {
    const queryClient = getQueryClient();

    const projects = await queryClient.fetchQuery({
        queryKey: ['projects'],
        queryFn: async () => {},
    });

    return (
        <HydrationBoundary state={dehydrate(queryClient)}>

        </HydrationBoundary>
    )
}
