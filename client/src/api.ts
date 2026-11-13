export async function api<T>(path: string): Promise<T> {
    const token = sessionStorage.getItem('token')
    const res = await fetch(path, {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
    })
    if (!res.ok) throw new Error(`Ошибка ${res.status}`)
    return res.json() as Promise<T>
}