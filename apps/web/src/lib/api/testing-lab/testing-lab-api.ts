// STUB: Testing Lab API (module disabled in backend)
// This is a stub to satisfy imports from disabled backend modules

export const testingLabApi = {
    async createSimpleTestingRequest(_data: any): Promise<any> {
        console.warn('Testing Lab API disabled: createSimpleTestingRequest called with stub');
        throw new Error('Testing Lab module is disabled. This functionality is not available.');
    },

    async getTestingRequests(): Promise<any[]> {
        console.warn('Testing Lab API disabled: getTestingRequests called with stub');
        return [];
    },

    async getTestingRequestById(_id: string): Promise<any> {
        console.warn('Testing Lab API disabled: getTestingRequestById called with stub');
        throw new Error('Testing Lab module is disabled. This functionality is not available.');
    },

    async updateTestingRequest(_id: string, _data: any): Promise<any> {
        console.warn('Testing Lab API disabled: updateTestingRequest called with stub');
        throw new Error('Testing Lab module is disabled. This functionality is not available.');
    },

    async deleteTestingRequest(_id: string): Promise<void> {
        console.warn('Testing Lab API disabled: deleteTestingRequest called with stub');
        throw new Error('Testing Lab module is disabled. This functionality is not available.');
    },
};
