declare module 'sql.js' {
    export interface QueryExecResult {
        columns: string[];
        values: Array<Array<number | string | null>>;
    }

    export interface SqlJsDatabase {
        run: (sql: string) => void;
        exec: (sql: string) => QueryExecResult[];
        close: () => void;
    }

    export interface SqlJsStatic {
        Database: new (data?: Uint8Array) => SqlJsDatabase;
    }

    interface InitSqlJsConfig {
        locateFile?: (file: string) => string;
    }

    export default function initSqlJs(config?: InitSqlJsConfig): Promise<SqlJsStatic>;
}
