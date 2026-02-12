import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { MsalProvider } from '@azure/msal-react'
import { msalInstance, initializeMsal } from './auth/msalConfig'
import './index.css'
import App from './App'

function renderApp() {
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <MsalProvider instance={msalInstance}>
        <App />
      </MsalProvider>
    </StrictMode>,
  );
}

initializeMsal()
  .then(renderApp)
  .catch((error) => {
    console.error("[MSAL] Initialization failed:", error);
    renderApp();
  });
