import {isServer, QueryClient} from "@tanstack/react-query";

const makeQueryClient = (): QueryClient => new QueryClient({
    defaultOptions: {
        queries: {
            staleTime: 60 * 1000, // 1 minute
        },
    },
});

let browserQueryClient: QueryClient | undefined = undefined;

export const getQueryClient = (): QueryClient => {
    if (isServer) {
        return makeQueryClient();
    } else {
        if (!browserQueryClient) browserQueryClient = makeQueryClient();
        return browserQueryClient;
    }
};