import { useState } from 'react';
import service from './service';

const AuthPage = ({ mode }) => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const isLogin = mode === 'login';

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    try {
      if (isLogin) {
        await service.login(username, password);
      } else {
        await service.register(username, password);
      }
      window.location.href = '/';
    } catch (err) {
      console.log(err.message);
      setError(isLogin ? 'שם משתמש או סיסמה שגויים' : (err.response?.data || 'ההרשמה נכשלה'));
    }
  };

  return (
    <section className="todoapp" style={{ padding: 20 }}>
      <h1>{isLogin ? 'login' : 'register'}</h1>
      <form onSubmit={handleSubmit}>
        <input
          className="new-todo"
          placeholder="שם משתמש"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
        />
        <input
          className="new-todo"
          type="password"
          placeholder="סיסמה"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        <button type="submit" style={{ margin: 10, padding: '5px 15px' }}>
          {isLogin ? 'התחברות' : 'הרשמה'}
        </button>
      </form>
      {error && <p style={{ color: 'red' }}>{error}</p>}
      <p>
        {isLogin ? (
          <a href="/register">משתמש חדש? להרשמה</a>
        ) : (
          <a href="/login">כבר רשומה? להתחברות</a>
        )}
      </p>
    </section>
  );
};

export default AuthPage;