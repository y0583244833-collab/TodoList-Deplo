import axios from 'axios';

axios.defaults.baseURL = process.env.REACT_APP_API_URL;
axios.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  }
);
axios.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error('API Error:', error.response ? error.response.data : error.message);

    if (error.response?.status === 401 && error.config?.url !== '/login') {
      localStorage.removeItem('token')
      window.location.href = '/login'
    }
    return Promise.reject(error);
  });



const service = {
  register: async (username, password) => {
    const { data } = await axios.post('/register', { username, password });
    localStorage.setItem('token', data.token);
  },

  login: async (username, password) => {
    const { data } = await axios.post('/login', { username, password });
    localStorage.setItem('token', data.token);
  },

  logout: () => {
    localStorage.removeItem('token');
    window.location.href = '/login';
  },
  getTasks: async () => {
    const result = await axios.get(`/items`)
    return result.data;
  },

  addTask: async (name) => {
    const result = await axios.post(`/items`, { name, isComplete: false });
    return result.data;
  },

  setCompleted: async (id, isComplete) => {
    const result = await axios.put(`/items/${id}`, { isComplete });
    return result.data;
  },

  deleteTask: async (id) => {
    const result = await axios.delete(`/items/${id}`);
    return result.data;
  }
};
export default service;
