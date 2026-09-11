import axios from 'axios';

const API_BASE_URL =  import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 8000,
});

// Interceptor to attach JWT Bearer token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('rescueplate_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export const authApi = {
  register: async (registerData) => {
    try {
      const response = await api.post('/auth/register', registerData);
      return response.data;
    } catch (error) {
      if (error.response?.data) {
        throw error.response.data;
      }
      throw { message: error.message || 'Network Error', isNetworkError: !error.response };
    }
  },

  login: async (loginData) => {
    try {
      const response = await api.post('/auth/login', loginData);
      return response.data;
    } catch (error) {
      if (error.response?.data) {
        throw error.response.data;
      }
      throw { message: error.message || 'Network Error', isNetworkError: !error.response };
    }
  },

  logout: async () => {
    try {
      await api.post('/auth/logout');
    } catch (e) {
      // Ignore network errors on logout
    }
    localStorage.removeItem('rescueplate_token');
    localStorage.removeItem('rescueplate_user');
  },

  getMyProfile: async () => {
    try {
      const response = await api.get('/profile/me');
      return response.data;
    } catch (error) {
      throw error.response?.data || { message: 'Failed to fetch profile' };
    }
  },

  updateMyProfile: async (updateData) => {
    try {
      const response = await api.put('/profile/me', updateData);
      return response.data;
    } catch (error) {
      throw error.response?.data || { message: 'Failed to update profile' };
    }
  },

  uploadProfilePicture: async (file) => {
    try {
      const formData = new FormData();
      formData.append('file', file);
      const response = await api.post('/profile/me/picture', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });
      return response.data;
    } catch (error) {
      throw error.response?.data || { message: 'Failed to upload profile picture' };
    }
  },

  removeProfilePicture: async () => {
    try {
      const response = await api.delete('/profile/me/picture');
      return response.data;
    } catch (error) {
      throw error.response?.data || { message: 'Failed to remove profile picture' };
    }
  },

  changePassword: async (passwordData) => {
    try {
      const response = await api.post('/auth/change-password', passwordData);
      return response.data;
    } catch (error) {
      throw error.response?.data || { message: 'Failed to change password' };
    }
  },

  deleteAccount: async (password) => {
    try {
      const response = await api.delete('/auth/account', { data: { password } });
      return response.data;
    } catch (error) {
      throw error.response?.data || { message: 'Failed to delete account' };
    }
  },

  getAdminUsers: async () => {
    try {
      const response = await api.get('/admin/users');
      return response.data;
    } catch (error) {
      throw error.response?.data || { message: 'Failed to fetch users' };
    }
  },

  toggleUserStatus: async (userId, isActive) => {
    try {
      const response = await api.patch(`/admin/users/${userId}/status`, { isActive });
      return response.data;
    } catch (error) {
      throw error.response?.data || { message: 'Failed to update status' };
    }
  }
};

export const getProfileImageUrl = (url) => {
  if (!url) return null;
  if (url.startsWith('http://') || url.startsWith('https://')) return url;
  const baseUrl = API_BASE_URL.replace(/\/api\/?$/, '');
  return `${baseUrl}${url.startsWith('/') ? '' : '/'}${url}`;
};

export default api;
