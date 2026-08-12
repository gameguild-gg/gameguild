import { Product, ProductProgram, Program, ProgramContent, ProgramContentType } from '@/lib/api/generated';

// Markdown content imports
import dataanalysisCustomerServicePerformanceReveal from './customer-service-performance/reveal.md';
import dataanalysisDataVisualizationReveal from './data-visualization/reveal.md';
import dataanalysisNumpyReveal from './numpy/reveal.md';
import dataanalysisPandasDataFrameReveal from './pandas-dataframe/reveal.md';
import dataanalysisPandasReveal from './pandas/reveal.md';
import dataanalysisSyllabus from './syllabus.md';

// Program definition
export const dataanalysisProgram: Program = {
    id: 'dataanalysis-program-1',
    title: 'Data Analysis',
    description:
        'Learn the fundamentals of data analysis using Python. This course covers data manipulation with pandas, visualization with matplotlib and seaborn, exploratory data analysis, and basic statistical methods.',
    slug: 'dataanalysis',
    thumbnail: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Data+Analysis',
    videoShowcaseUrl: null,
    estimatedHours: 40,
    enrollmentStatus: 0, // Open
    maxEnrollments: null,
    enrollmentDeadline: null,
    category: 0, // Programming
    difficulty: 0, // Beginner
    visibility: 0, // Public
    status: 1, // Published
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    programContents: [],
    programUsers: [],
    programRatings: [],
    programWishlists: [],
};

// Product definition
export const dataanalysisProduct: Product = {
    id: 'dataanalysis-product-1',
    title: 'Data Analysis Course',
    name: 'Data Analysis',
    description: 'Learn data analysis fundamentals with Python, pandas, and visualization libraries',
    shortDescription: 'Master data analysis, visualization, and statistical methods with Python',
    imageUrl: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Data+Analysis',
    type: 0, // Course
    isBundle: false,
    creatorId: '1',
    bundleItems: null,
    referralCommissionPercentage: 0,
    maxAffiliateDiscount: 0,
    affiliateCommissionPercentage: 0,
    visibility: 0, // Public
    status: 1, // Published
    slug: 'dataanalysis',
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    productPrograms: [],
    productPricings: [],
    subscriptionPlans: [],
    userProducts: [],
    promoCodes: [],
};

// Product-Program relation
export const dataanalysisProductProgram: ProductProgram = {
    id: 'dataanalysis-product-program-1',
    productId: 'dataanalysis-product-1',
    product: dataanalysisProduct,
    programId: 'dataanalysis-program-1',
    program: dataanalysisProgram,
    sortOrder: 1,
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Program Contents
export const dataanalysisSyllabusContent: ProgramContent = {
    id: 'dataanalysis-syllabus',
    programId: 'dataanalysis-program-1',
    parentId: undefined,
    title: 'Course Syllabus',
    description: 'Data Analysis course overview and objectives',
    type: 0, // Page
    body: dataanalysisSyllabus,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: dataanalysisProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dataanalysisNumpyContent: ProgramContent = {
    id: 'dataanalysis-numpy',
    programId: 'dataanalysis-program-1',
    parentId: undefined,
    title: 'NumPy: Numerical Python',
    description: 'Introduction to NumPy arrays, operations, and numerical computing',
    type: ProgramContentType.REVEAL,
    body: dataanalysisNumpyReveal,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: dataanalysisProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dataanalysisPandasContent: ProgramContent = {
    id: 'dataanalysis-pandas',
    programId: 'dataanalysis-program-1',
    parentId: undefined,
    title: 'Pandas: Python Data Analysis',
    description: 'Introduction to Pandas Series and DataFrames for data manipulation',
    type: ProgramContentType.REVEAL,
    body: dataanalysisPandasReveal,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: dataanalysisProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dataanalysisPandasDataFrameContent: ProgramContent = {
    id: 'dataanalysis-pandas-dataframe',
    programId: 'dataanalysis-program-1',
    parentId: undefined,
    title: 'Pandas: DataFrames',
    description: 'Two-dimensional data structures: creating, inspecting, selecting, modifying, and analyzing DataFrames',
    type: ProgramContentType.REVEAL,
    body: dataanalysisPandasDataFrameReveal,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: dataanalysisProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dataanalysisCustomerServicePerformanceContent: ProgramContent = {
    id: 'dataanalysis-customer-service-performance',
    programId: 'dataanalysis-program-1',
    parentId: undefined,
    title: 'Customer Support Performance Analysis',
    description: 'Exploratory data analysis using a real customer service dataset: correlation, segmentation, and theme-based insight generation',
    type: ProgramContentType.REVEAL,
    body: dataanalysisCustomerServicePerformanceReveal,
    sortOrder: 5,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: dataanalysisProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dataanalysisDataVisualizationContent: ProgramContent = {
    id: 'dataanalysis-data-visualization',
    programId: 'dataanalysis-program-1',
    parentId: undefined,
    title: 'Data Visualization with Plotly Express',
    description: 'Interactive data visualization using Plotly Express: scatter plots, line charts, bar charts, histograms, maps, animations, and customization',
    type: ProgramContentType.REVEAL,
    body: dataanalysisDataVisualizationReveal,
    sortOrder: 6,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: dataanalysisProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Wire program contents
dataanalysisProgram.programContents = [
    dataanalysisSyllabusContent,
    dataanalysisNumpyContent,
    dataanalysisPandasContent,
    dataanalysisPandasDataFrameContent,
    dataanalysisCustomerServicePerformanceContent,
    dataanalysisDataVisualizationContent,
];
