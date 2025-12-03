/**
 * Stub exports for modals module.
 * These components are disabled in production.
 */

'use client';

import React from 'react';

export interface ModalProps {
    isOpen: boolean;
    onClose: () => void;
    title?: string;
    children?: React.ReactNode;
}

export function Modal({ isOpen, onClose, title, children }: ModalProps) {
    if (!isOpen) return null;
    return (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50" onClick={onClose}>
            <div className="bg-white rounded-lg p-6 max-w-md" onClick={(e) => e.stopPropagation()}>
                {title && <h2 className="text-lg font-semibold mb-4">{title}</h2>}
                {children}
            </div>
        </div>
    );
}

export function ConfirmModal(props: ModalProps & { onConfirm?: () => void }) {
    return <Modal {...props} />;
}

export function AlertModal(props: ModalProps) {
    return <Modal {...props} />;
}

export interface AppearanceModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSave?: (settings: unknown) => void;
}

export function AppearanceModal({ isOpen, onClose }: AppearanceModalProps) {
    return <Modal isOpen={isOpen} onClose={onClose} title="Appearance Settings">
        <p className="text-slate-500">Appearance settings disabled</p>
    </Modal>;
}

export default { Modal, ConfirmModal, AlertModal, AppearanceModal };
