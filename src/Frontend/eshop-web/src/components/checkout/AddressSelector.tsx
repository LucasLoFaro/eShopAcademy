import type { SavedAddress } from "../../types";

interface AddressSelectorProps {
  addresses: SavedAddress[];
  selectedAddressId?: string;
  onSelect: (address: SavedAddress) => void;
  onEdit: (addressId: string) => void;
  onDelete: (addressId: string) => void;
  onAddNew: () => void;
}

export default function AddressSelector({
  addresses,
  selectedAddressId,
  onSelect,
  onEdit,
  onDelete,
  onAddNew,
}: AddressSelectorProps) {
  const formatAddress = (address: SavedAddress): string => {
    const parts = [
      `${address.address.street} ${address.address.number}`,
      address.address.additionalInformation,
      `${address.address.zipCode} ${address.address.city}`,
      address.address.state,
      address.address.country,
    ].filter(Boolean);
    return parts.join(", ");
  };

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-medium text-gray-700">Saved Addresses</h3>
        <button
          onClick={onAddNew}
          className="text-sm font-medium text-amber-600 hover:text-amber-700 flex items-center gap-1"
        >
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          Add New Address
        </button>
      </div>

      {addresses.length === 0 ? (
        <div className="text-center py-8 bg-gray-50 rounded-lg border-2 border-dashed border-gray-300">
          <svg
            className="mx-auto h-12 w-12 text-gray-400"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"
            />
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"
            />
          </svg>
          <p className="mt-2 text-sm text-gray-500">No saved addresses</p>
          <button
            onClick={onAddNew}
            className="mt-3 text-sm font-medium text-amber-600 hover:text-amber-700"
          >
            Add your first address
          </button>
        </div>
      ) : (
        <div className="space-y-2">
          {addresses.map((address) => (
            <button
              key={address.id}
              onClick={() => onSelect(address)}
              className={`w-full text-left rounded-lg border-2 p-4 transition-all ${
                selectedAddressId === address.id
                  ? "border-amber-500 bg-amber-50"
                  : "border-gray-200 hover:border-gray-300 bg-white"
              }`}
            >
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <span className="font-semibold text-gray-900">{address.description}</span>
                    {address.isDefault && (
                      <span className="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-700">
                        Default
                      </span>
                    )}
                  </div>
                  <p className="text-sm text-gray-600">{formatAddress(address)}</p>
                </div>

                {selectedAddressId === address.id && (
                  <div className="flex items-center gap-2 ml-3">
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        onEdit(address.id);
                      }}
                      className="rounded-full p-1.5 text-gray-500 hover:bg-gray-100 hover:text-gray-700 transition-colors"
                      title="Edit address"
                    >
                      <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                        />
                      </svg>
                    </button>
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        onDelete(address.id);
                      }}
                      className="rounded-full p-1.5 text-red-500 hover:bg-red-50 hover:text-red-700 transition-colors"
                      title="Delete address"
                    >
                      <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                        />
                      </svg>
                    </button>
                  </div>
                )}
              </div>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
