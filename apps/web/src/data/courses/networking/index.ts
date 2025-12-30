import { Product, ProductProgram, Program, ProgramContent } from '@/lib/api/generated';

import networkingSyllabus from './syllabus.md';

export const networkingProgram: Program = {
	id: 'networking-program-1',
	title: 'Game Network Programming with C++',
	description:
		'Learn to design, implement, and optimize real-time networked applications and games using sockets, serialization, synchronization, and performance tuning techniques.',
	slug: 'networking',
	thumbnail: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Networking',
	videoShowcaseUrl: null,
	estimatedHours: 60,
	enrollmentStatus: 0, // Open
	maxEnrollments: null,
	enrollmentDeadline: null,
	category: 1, // Game Development / Networking
	difficulty: 1, // Intermediate
	visibility: 0, // Public
	status: 1, // Published
	createdAt: '2023-01-01T00:00:00Z',
	updatedAt: '2023-01-01T00:00:00Z',
	programContents: [],
	programUsers: [],
	programRatings: [],
	programWishlists: [],
};

export const networkingProduct: Product = {
	id: 'networking-product-1',
	title: 'Game Network Programming with C++ Course',
	name: 'Game Network Programming with C++',
	description:
		'Build multiplayer-ready applications with sockets, serialization, prediction/interpolation, reliability, and security fundamentals.',
	shortDescription: 'Hands-on game networking: sockets, sync, reliability, security.',
	imageUrl: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Networking',
	type: 0, // Course
	isBundle: false,
	creatorId: '1',
	bundleItems: null,
	referralCommissionPercentage: 0,
	maxAffiliateDiscount: 0,
	affiliateCommissionPercentage: 0,
	visibility: 0, // Public
	status: 1, // Published
	slug: 'networking',
	createdAt: '2023-01-01T00:00:00Z',
	updatedAt: '2023-01-01T00:00:00Z',
	productPrograms: [],
	productPricings: [],
	subscriptionPlans: [],
	userProducts: [],
	promoCodes: [],
};

export const networkingProductProgram: ProductProgram = {
	id: 'networking-product-program-1',
	productId: 'networking-product-1',
	product: networkingProduct,
	programId: 'networking-program-1',
	program: networkingProgram,
	sortOrder: 1,
	createdAt: '2023-01-01T00:00:00Z',
	updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingSyllabusContent: ProgramContent = {
	id: 'networking-syllabus',
	programId: 'networking-program-1',
	parentId: undefined,
	title: 'Course Syllabus',
	description: 'Networking course overview, objectives, schedule, and policies.',
	type: 0, // Page
	body: networkingSyllabus,
	sortOrder: 1,
	isRequired: true,
	gradingMethod: 0, // None
	maxPoints: null,
	estimatedMinutes: 45,
	visibility: 1, // Published
	program: networkingProgram,
	parent: undefined,
	children: [],
	contentInteractions: [],
	createdAt: '2023-01-01T00:00:00Z',
	updatedAt: '2023-01-01T00:00:00Z',
};

networkingProgram.programContents = [networkingSyllabusContent];
networkingProduct.productPrograms = [networkingProductProgram];

export default networkingProgram;
