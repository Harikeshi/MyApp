import { deleteJson, getJson, postJson, putJson } from "./http";

export interface UserRef {
  id: string;
  name: string;
}

export interface Project {
  id: number;
  name: string;
  ownerId: string;
  ownerName: string;
  isOwner: boolean;
  /** Ответственные (владелец в список не входит). */
  members: UserRef[];
}

export interface ProjectInput {
  name: string;
  memberIds: string[];
}

export const fetchProjects = (signal?: AbortSignal) => getJson<Project[]>("/projects", signal);

export const createProject = (input: ProjectInput) => postJson<Project>("/projects", input);

export const updateProject = (id: number, input: ProjectInput) => putJson<Project>(`/projects/${id}`, input);

export const removeProject = (id: number) => deleteJson(`/projects/${id}`);

/** Поиск пользователей для списка ответственных: по имени или точной почте, от 2 символов. */
export const searchUsers = (query: string, signal?: AbortSignal) =>
  getJson<UserRef[]>(`/users?search=${encodeURIComponent(query)}`, signal);
