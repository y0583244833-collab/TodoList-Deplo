import App from './App';
import AuthPage from './AuthPage';
import service from './service';

const Root = () => {
  const path = window.location.pathname;
  const token = localStorage.getItem('token');

  if (path === '/login') return <AuthPage mode="login" />;
  if (path === '/register') return <AuthPage mode="register" />;

  if (!token) {
    window.location.href = '/login';
    return null;
  }

  return (
    <>
      <button onClick={service.logout} style={{ margin: 10 }}>התנתקות</button>
      <App />
    </>
  );
};

export default Root;