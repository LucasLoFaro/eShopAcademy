const CLIENT_ID_KEY = "eshop_client_id";

function getOrCreateClientId(): string {
  let id = localStorage.getItem(CLIENT_ID_KEY);
  if (!id) {
    id = crypto.randomUUID();
    localStorage.setItem(CLIENT_ID_KEY, id);
  }
  return id;
}

export function useClientId(): string {
  return getOrCreateClientId();
}
