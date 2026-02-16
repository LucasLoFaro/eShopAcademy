import { useState } from "react";
import type { Address, SavedAddress } from "../../types";
import AddressSelector from "./AddressSelector";

interface AddressStepProps {
  address?: Address;
  savedAddresses?: SavedAddress[];
  onNext: (address: Address, description?: string, saveAddress?: boolean) => void;
  onDeleteAddress?: (addressId: string) => Promise<void>;
  onBack?: () => void;
}

type ViewMode = "select" | "form";

export default function AddressStep({ 
  address: initialAddress, 
  savedAddresses = [],
  onNext,
  onDeleteAddress
}: AddressStepProps) {
  const [viewMode, setViewMode] = useState<ViewMode>(
    savedAddresses.length > 0 ? "select" : "form"
  );
  const [selectedSavedAddress, setSelectedSavedAddress] = useState<SavedAddress | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState<string | null>(null);
  
  const [address, setAddress] = useState<Address>(
    initialAddress ?? {
      street: "",
      number: "",
      additionalInformation: "",
      zipCode: "",
      city: "",
      state: "",
      country: "",
    }
  );
  const [addressDescription, setAddressDescription] = useState("");
  const [saveForLater, setSaveForLater] = useState(false);

  const [errors, setErrors] = useState<Record<string, string>>({});

  const handleSelectSavedAddress = (savedAddr: SavedAddress) => {
    setSelectedSavedAddress(savedAddr);
    setAddress(savedAddr.address);
  };

  const handleEditSavedAddress = (addressId: string) => {
    const addressToEdit = savedAddresses.find(a => a.id === addressId);
    if (addressToEdit) {
      setAddress(addressToEdit.address);
      setAddressDescription(addressToEdit.description);
      setIsEditing(true);
      setViewMode("form");
    }
  };

  const handleDeleteAddress = (addressId: string) => {
    setShowDeleteConfirm(addressId);
  };

  const confirmDelete = async () => {
    if (showDeleteConfirm && onDeleteAddress) {
      try {
        await onDeleteAddress(showDeleteConfirm);
        // If we deleted the currently selected address, clear selection
        if (selectedSavedAddress?.id === showDeleteConfirm) {
          setSelectedSavedAddress(null);
          setAddress({
            street: "",
            number: "",
            additionalInformation: "",
            zipCode: "",
            city: "",
            state: "",
            country: "",
          });
        }
      } catch (error) {
        console.error("Failed to delete address:", error);
      }
    }
    setShowDeleteConfirm(null);
  };

  const handleAddNew = () => {
    setSelectedSavedAddress(null);
    setIsEditing(false);
    setAddress({
      street: "",
      number: "",
      additionalInformation: "",
      zipCode: "",
      city: "",
      state: "",
      country: "",
    });
    setAddressDescription("");
    setViewMode("form");
  };

  const handleChange = (field: keyof Address, value: string) => {
    setAddress((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!address.street.trim()) newErrors.street = "Street is required";
    if (!address.number.trim()) newErrors.number = "Number is required";
    if (!address.zipCode.trim()) newErrors.zipCode = "Zip code is required";
    if (!address.city.trim()) newErrors.city = "City is required";
    if (!address.country.trim()) newErrors.country = "Country is required";
    
    if (saveForLater && !addressDescription.trim()) {
      newErrors.description = "Description is required when saving address";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (selectedSavedAddress && !isEditing) {
      // Using a saved address without modification
      onNext(selectedSavedAddress.address);
      return;
    }

    if (validateForm()) {
      onNext(address, saveForLater ? addressDescription : undefined, saveForLater);
    }
  };

  const getMapUrl = () => {
    if (!address.street || !address.city) return null;
    
    const addressString = [
      address.number,
      address.street,
      address.city,
      address.state,
      address.zipCode,
      address.country,
    ].filter(Boolean).join(", ");
    
    const encodedAddress = encodeURIComponent(addressString);
    return `https://www.google.com/maps/embed/v1/place?key=AIzaSyBFw0Qbyq9zTFTd-tUY6dZWTgaQzuU17R8&q=${encodedAddress}&zoom=15`;
  };

  const mapUrl = getMapUrl();
  const isFormDisabled = selectedSavedAddress !== null && !isEditing;

  return (
    <div>
      <h2 className="mb-6 text-xl font-semibold">Shipping Address</h2>
      
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Left Column - Address Selection/Form */}
        <div className="space-y-6">
          {/* Address Selector */}
          {viewMode === "select" && (
            <AddressSelector
              addresses={savedAddresses}
              selectedAddressId={selectedSavedAddress?.id}
              onSelect={handleSelectSavedAddress}
              onEdit={handleEditSavedAddress}
              onDelete={handleDeleteAddress}
              onAddNew={handleAddNew}
            />
          )}

          {/* Address Form */}
          {viewMode === "form" && (
            <>
              {savedAddresses.length > 0 && (
                <button
                  onClick={() => setViewMode("select")}
                  className="text-sm text-amber-600 hover:text-amber-700 font-medium flex items-center gap-1"
                >
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
                  </svg>
                  Back to saved addresses
                </button>
              )}

              <form onSubmit={handleSubmit} className="space-y-4">
                {/* Save Address Option */}
                {!isEditing && (
                  <div className="rounded-lg bg-blue-50 border border-blue-200 p-4">
                    <label className="flex items-start gap-3 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={saveForLater}
                        onChange={(e) => setSaveForLater(e.target.checked)}
                        className="mt-1 h-4 w-4 text-amber-500 focus:ring-amber-500 border-gray-300 rounded"
                      />
                      <div>
                        <p className="font-medium text-gray-900 text-sm">Save this address for later</p>
                        <p className="text-xs text-gray-600 mt-0.5">
                          Quickly select it in future checkouts
                        </p>
                      </div>
                    </label>

                    {saveForLater && (
                      <div className="mt-3">
                        <label className="block text-sm font-medium text-gray-700 mb-1">
                          Address Label <span className="text-red-500">*</span>
                        </label>
                        <input
                          type="text"
                          value={addressDescription}
                          onChange={(e) => setAddressDescription(e.target.value)}
                          className={`w-full rounded-lg border px-3 py-2 focus:outline-none focus:ring-2 ${
                            errors.description ? "border-red-500 focus:ring-red-500" : "border-gray-300 focus:ring-amber-500"
                          }`}
                          placeholder="e.g., Home, Work, Office"
                        />
                        {errors.description && <p className="mt-1 text-xs text-red-500">{errors.description}</p>}
                      </div>
                    )}
                  </div>
                )}

                {/* Street and Number */}
                <div className="grid grid-cols-3 gap-3">
                  <div className="col-span-2">
                    <label className="mb-1 block text-sm font-medium text-gray-700">
                      Street <span className="text-red-500">*</span>
                    </label>
                    <input
                      type="text"
                      value={address.street}
                      onChange={(e) => handleChange("street", e.target.value)}
                      disabled={isFormDisabled}
                      className={`w-full rounded-lg border px-3 py-2 focus:outline-none focus:ring-2 ${
                        isFormDisabled ? "bg-gray-100 cursor-not-allowed" :
                        errors.street ? "border-red-500 focus:ring-red-500" : "border-gray-300 focus:ring-amber-500"
                      }`}
                      placeholder="e.g., Main Street"
                    />
                    {errors.street && <p className="mt-1 text-xs text-red-500">{errors.street}</p>}
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">
                      Number <span className="text-red-500">*</span>
                    </label>
                    <input
                      type="text"
                      value={address.number}
                      onChange={(e) => handleChange("number", e.target.value)}
                      disabled={isFormDisabled}
                      className={`w-full rounded-lg border px-3 py-2 focus:outline-none focus:ring-2 ${
                        isFormDisabled ? "bg-gray-100 cursor-not-allowed" :
                        errors.number ? "border-red-500 focus:ring-red-500" : "border-gray-300 focus:ring-amber-500"
                      }`}
                      placeholder="123"
                    />
                    {errors.number && <p className="mt-1 text-xs text-red-500">{errors.number}</p>}
                  </div>
                </div>

                {/* Additional Info */}
                <div>
                  <label className="mb-1 block text-sm font-medium text-gray-700">
                    Additional Information
                  </label>
                  <input
                    type="text"
                    value={address.additionalInformation}
                    onChange={(e) => handleChange("additionalInformation", e.target.value)}
                    disabled={isFormDisabled}
                    className={`w-full rounded-lg border px-3 py-2 focus:outline-none focus:ring-2 ${
                      isFormDisabled ? "bg-gray-100 cursor-not-allowed" : "border-gray-300 focus:ring-amber-500"
                    }`}
                    placeholder="Apartment, suite, etc. (optional)"
                  />
                </div>

                {/* Zip and City */}
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">
                      Zip Code <span className="text-red-500">*</span>
                    </label>
                    <input
                      type="text"
                      value={address.zipCode}
                      onChange={(e) => handleChange("zipCode", e.target.value)}
                      disabled={isFormDisabled}
                      className={`w-full rounded-lg border px-3 py-2 focus:outline-none focus:ring-2 ${
                        isFormDisabled ? "bg-gray-100 cursor-not-allowed" :
                        errors.zipCode ? "border-red-500 focus:ring-red-500" : "border-gray-300 focus:ring-amber-500"
                      }`}
                      placeholder="12345"
                    />
                    {errors.zipCode && <p className="mt-1 text-xs text-red-500">{errors.zipCode}</p>}
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">
                      City <span className="text-red-500">*</span>
                    </label>
                    <input
                      type="text"
                      value={address.city}
                      onChange={(e) => handleChange("city", e.target.value)}
                      disabled={isFormDisabled}
                      className={`w-full rounded-lg border px-3 py-2 focus:outline-none focus:ring-2 ${
                        isFormDisabled ? "bg-gray-100 cursor-not-allowed" :
                        errors.city ? "border-red-500 focus:ring-red-500" : "border-gray-300 focus:ring-amber-500"
                      }`}
                      placeholder="New York"
                    />
                    {errors.city && <p className="mt-1 text-xs text-red-500">{errors.city}</p>}
                  </div>
                </div>

                {/* State and Country */}
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">State</label>
                    <input
                      type="text"
                      value={address.state ?? ""}
                      onChange={(e) => handleChange("state", e.target.value)}
                      disabled={isFormDisabled}
                      className={`w-full rounded-lg border px-3 py-2 focus:outline-none focus:ring-2 ${
                        isFormDisabled ? "bg-gray-100 cursor-not-allowed" : "border-gray-300 focus:ring-amber-500"
                      }`}
                      placeholder="NY (optional)"
                    />
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">
                      Country <span className="text-red-500">*</span>
                    </label>
                    <input
                      type="text"
                      value={address.country}
                      onChange={(e) => handleChange("country", e.target.value)}
                      disabled={isFormDisabled}
                      className={`w-full rounded-lg border px-3 py-2 focus:outline-none focus:ring-2 ${
                        isFormDisabled ? "bg-gray-100 cursor-not-allowed" :
                        errors.country ? "border-red-500 focus:ring-red-500" : "border-gray-300 focus:ring-amber-500"
                      }`}
                      placeholder="United States"
                    />
                    {errors.country && <p className="mt-1 text-xs text-red-500">{errors.country}</p>}
                  </div>
                </div>

                <button
                  type="submit"
                  className="w-full rounded-full bg-amber-400 py-3 font-semibold text-gray-900 hover:bg-amber-500 transition-colors mt-6"
                >
                  Continue to Payment
                </button>
              </form>
            </>
          )}

          {/* Submit button for selected address */}
          {viewMode === "select" && selectedSavedAddress && (
            <button
              onClick={handleSubmit}
              className="w-full rounded-full bg-amber-400 py-3 font-semibold text-gray-900 hover:bg-amber-500 transition-colors"
            >
              Continue to Payment
            </button>
          )}
        </div>

        {/* Right Column - Map */}
        <div>
          <div className="sticky top-4">
            <h3 className="mb-3 text-sm font-medium text-gray-700">Address Preview</h3>
            <div className="overflow-hidden rounded-lg border border-gray-300 bg-gray-100">
              {mapUrl ? (
                <iframe
                  title="Address Map"
                  src={mapUrl}
                  className="h-96 w-full"
                  style={{ border: 0 }}
                  loading="lazy"
                  referrerPolicy="no-referrer-when-downgrade"
                />
              ) : (
                <div className="flex h-96 items-center justify-center text-sm text-gray-500">
                  <div className="text-center">
                    <svg
                      className="mx-auto h-12 w-12 text-gray-400 mb-2"
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
                    <p>Select or enter an address to preview location</p>
                  </div>
                </div>
              )}
            </div>
            {mapUrl && (
              <p className="mt-2 text-xs text-gray-500">
                ?? Please verify this is the correct delivery location
              </p>
            )}
          </div>
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      {showDeleteConfirm && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Delete Address?</h3>
            <p className="text-sm text-gray-600 mb-6">
              Are you sure you want to delete this address? This action cannot be undone.
            </p>
            <div className="flex gap-3">
              <button
                onClick={() => setShowDeleteConfirm(null)}
                className="flex-1 rounded-lg border-2 border-gray-300 py-2 font-medium text-gray-700 hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={confirmDelete}
                className="flex-1 rounded-lg bg-red-600 py-2 font-medium text-white hover:bg-red-700"
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
