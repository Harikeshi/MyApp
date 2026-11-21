import { useAuth } from "./features/auth/AuthContext";
import AuthPage from "./features/auth/AuthPage";
import ResetPassword from "./features/auth/ResetPassword";
import Layout from "./Layout";

export default function App() {
  const { status, resetParams } = useAuth();

  if (resetParams) return <ResetPassword />;     // ссылка из письма: сначала новый пароль
  if (status === "loading") return null;         // проверяем сохранённую сессию
  if (status === "anon") return <AuthPage />;

  // выход размонтирует Layout вместе с данными прошлого пользователя
  return <Layout />;
}
 