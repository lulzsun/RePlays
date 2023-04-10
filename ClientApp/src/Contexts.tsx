import { createContext } from "react";

export const ContextMenuContext = createContext<ContextMenuOptions | null>(null);
export const ModalContext = createContext<ModalOptions | null>(null);