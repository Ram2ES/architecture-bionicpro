import React, { useState } from 'react';
import { useKeycloak } from '@react-keycloak/web';

const ReportPage: React.FC = () => {
  const { keycloak, initialized } = useKeycloak();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const getUserIdFromToken = (): string | null => {
    if (!keycloak?.tokenParsed) return null;
    const systemUserId = (keycloak.tokenParsed as any).system_user_id;
    return systemUserId || null;
  };

  const downloadReport = async () => {
    if (!keycloak?.token) {
      setError('Не авторизован');
      return;
    }

    const userId = getUserIdFromToken();
    if (!userId) {
      setError('ID пользователя не найден в токене. Пожалуйста, выйдите и войдите снова.');
      return;
    }

    try {
      setLoading(true);
      setError(null);

      const apiUrl = process.env.REACT_APP_API_URL || 'http://localhost:5000';
      const response = await fetch(`${apiUrl}/api/reports/user/${userId}/download`, {
        headers: {
          'Authorization': `Bearer ${keycloak.token}`
        }
      });

      if (!response.ok) {
        if (response.status === 401) {
          throw new Error('Не авторизован. Пожалуйста, войдите снова.');
        } else if (response.status === 403) {
          throw new Error('Доступ запрещён.');
        } else if (response.status === 404) {
          throw new Error('Отчёты для вашей учётной записи не найдены.');
        } else {
          throw new Error(`Ошибка: ${response.status} ${response.statusText}`);
        }
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;

      const contentDisposition = response.headers.get('Content-Disposition');
      let fileName = 'BionicPRO_Report.md';
      if (contentDisposition) {
        const fileNameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
        if (fileNameMatch && fileNameMatch[1]) {
          fileName = fileNameMatch[1].replace(/['"]/g, '');
        }
      }

      a.download = fileName;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);

    } catch (err) {
      setError(err instanceof Error ? err.message : 'Произошла ошибка при загрузке отчёта');
      console.error('Error downloading report:', err);
    } finally {
      setLoading(false);
    }
  };

  if (!initialized) {
    return <div>Загрузка...</div>;
  }

  if (!keycloak.authenticated) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen bg-gray-100">
        <button
          onClick={() => keycloak.login()}
          className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
        >
          Войти
        </button>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-100">
      <div className="max-w-4xl mx-auto p-6">
        {/* Header */}
        <div className="bg-white rounded-lg shadow-md p-6 mb-6">
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-3xl font-bold text-gray-800">BionicPRO - Отчёты</h1>
              <p className="text-gray-600 mt-2">
                Добро пожаловать, {keycloak.tokenParsed?.name || keycloak.tokenParsed?.preferred_username}!
              </p>
            </div>
            <button
              onClick={() => keycloak.logout()}
              className="px-4 py-2 bg-gray-500 text-white rounded hover:bg-gray-600 transition-colors"
            >
              Выйти
            </button>
          </div>
        </div>

        {/* Main Content */}
        <div className="bg-white rounded-lg shadow-md p-8">
          <div className="text-center">
            <svg
              className="mx-auto h-24 w-24 text-blue-500 mb-4"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
              />
            </svg>

            <h2 className="text-2xl font-semibold text-gray-800 mb-2">
              Отчёт об использовании протеза
            </h2>
            <p className="text-gray-600 mb-8">
              Сгенерируйте и скачайте подробный отчёт об использовании протеза, его производительности и показателях здоровья.
            </p>

            <button
              onClick={downloadReport}
              disabled={loading}
              className={`px-8 py-4 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors font-semibold text-lg flex items-center justify-center mx-auto ${
                loading ? 'opacity-50 cursor-not-allowed' : ''
              }`}
            >
              {loading ? (
                <>
                  <svg
                    className="animate-spin -ml-1 mr-3 h-5 w-5 text-white"
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                  >
                    <circle
                      className="opacity-25"
                      cx="12"
                      cy="12"
                      r="10"
                      stroke="currentColor"
                      strokeWidth="4"
                    ></circle>
                    <path
                      className="opacity-75"
                      fill="currentColor"
                      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                    ></path>
                  </svg>
                  Генерация отчёта...
                </>
              ) : (
                <>
                  <svg
                    className="mr-2 h-6 w-6"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                    />
                  </svg>
                  Скачать отчёт (Markdown)
                </>
              )}
            </button>

            {error && (
              <div className="mt-6 p-4 bg-red-100 text-red-700 rounded-lg border border-red-300">
                <div className="flex items-center">
                  <svg
                    className="h-5 w-5 mr-2"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                  >
                    <path
                      fillRule="evenodd"
                      d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                      clipRule="evenodd"
                    />
                  </svg>
                  <strong>Ошибка:</strong>&nbsp;{error}
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default ReportPage;