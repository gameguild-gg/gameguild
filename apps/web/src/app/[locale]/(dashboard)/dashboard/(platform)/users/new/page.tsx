import { redirect } from 'next/navigation';
import { createUser } from '@/lib/user-management/users/users.actions';
import { UserForm } from '@/components/users/user-form';

export const metadata = {
	title: 'Create User | Game Guild Dashboard',
};

export default async function NewUserPage() {
	async function handleCreateUser(formData: FormData) {
		'use server';
		const name = formData.get('name') as string;
		const email = formData.get('email') as string;
		const role = formData.get('role') as string;
		await createUser({ name, email, role });
		redirect('../');
	}

	return (
		<div className="container mx-auto px-4 py-8">
			<h1 className="text-2xl font-bold mb-4">Create New User</h1>
			<UserForm onSubmit={handleCreateUser} />
		</div>
	);
}
