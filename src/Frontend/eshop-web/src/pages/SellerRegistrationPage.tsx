import { useState, useRef } from "react";
import { useAnalyzeDocument, useRegisterSeller } from "../hooks/useSeller";
import type { DocumentAnalysisResult, SellerAddress } from "../types";

export default function SellerRegistrationPage() {
  const analyzeDocument = useAnalyzeDocument();
  const registerSeller = useRegisterSeller();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [step, setStep] = useState<"upload" | "confirm" | "success">("upload");
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [extractedData, setExtractedData] = useState<DocumentAnalysisResult | null>(null);

  // Editable form fields
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [taxId, setTaxId] = useState("");
  const [address, setAddress] = useState<SellerAddress>({
    street: "",
    number: "",
    additionalInformation: "",
    zipCode: "",
    city: "",
    state: "",
    country: "",
  });

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setSelectedFile(file);
    }
  };

  const handleUpload = async () => {
    if (!selectedFile) return;

    analyzeDocument.mutate(selectedFile, {
      onSuccess: (data) => {
        setExtractedData(data);
        setName(data.name);
        setEmail(data.email);
        setTaxId(data.taxId);
        setAddress(data.address);
        setStep("confirm");
      },
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    registerSeller.mutate(
      {
        name,
        email,
        taxId,
        address,
        documentUrl: selectedFile?.name ?? "",
      },
      {
        onSuccess: () => {
          setStep("success");
        },
      }
    );
  };

  if (step === "success") {
    return (
      <div className="max-w-2xl mx-auto py-12 px-4">
        <div className="bg-green-50 border border-green-200 rounded-xl p-8 text-center">
          <div className="text-5xl mb-4">🎉</div>
          <h2 className="text-2xl font-bold text-green-800 mb-2">Registration Submitted!</h2>
          <p className="text-green-700">
            Your seller registration has been submitted for review. You'll be notified once your account is approved.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-3xl mx-auto py-8 px-4">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Become a Seller</h1>
        <p className="mt-2 text-gray-600">
          Start selling on eShop Academy. Upload your tax registration document and we'll extract your business information.
        </p>
      </div>

      {/* Progress indicator */}
      <div className="flex items-center gap-4 mb-8">
        <div className={`flex items-center gap-2 ${step === "upload" ? "text-amber-600 font-semibold" : "text-gray-400"}`}>
          <span className={`flex h-8 w-8 items-center justify-center rounded-full text-sm font-bold ${step === "upload" ? "bg-amber-400 text-gray-900" : "bg-gray-200 text-gray-500"}`}>1</span>
          <span className="text-sm">Upload Document</span>
        </div>
        <div className="flex-1 h-px bg-gray-300" />
        <div className={`flex items-center gap-2 ${step === "confirm" ? "text-amber-600 font-semibold" : "text-gray-400"}`}>
          <span className={`flex h-8 w-8 items-center justify-center rounded-full text-sm font-bold ${step === "confirm" ? "bg-amber-400 text-gray-900" : "bg-gray-200 text-gray-500"}`}>2</span>
          <span className="text-sm">Confirm Details</span>
        </div>
      </div>

      {step === "upload" && (
        <div className="bg-white border border-gray-200 rounded-xl p-8">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Upload Tax Registration Document</h2>
          <p className="text-sm text-gray-500 mb-6">
            Upload an image or PDF of your tax registration document. We'll automatically extract your business information.
          </p>

          <div
            onClick={() => fileInputRef.current?.click()}
            className="border-2 border-dashed border-gray-300 rounded-lg p-8 text-center cursor-pointer hover:border-amber-400 hover:bg-amber-50 transition"
          >
            <input
              ref={fileInputRef}
              type="file"
              accept="image/*,.pdf"
              onChange={handleFileSelect}
              className="hidden"
            />
            {selectedFile ? (
              <div>
                <div className="text-4xl mb-2">📄</div>
                <p className="text-sm font-medium text-gray-900">{selectedFile.name}</p>
                <p className="text-xs text-gray-500 mt-1">
                  {(selectedFile.size / 1024 / 1024).toFixed(2)} MB
                </p>
                <p className="text-xs text-amber-600 mt-2">Click to choose a different file</p>
              </div>
            ) : (
              <div>
                <div className="text-4xl mb-2">📎</div>
                <p className="text-sm font-medium text-gray-700">Click to upload your document</p>
                <p className="text-xs text-gray-500 mt-1">Supports images and PDF files</p>
              </div>
            )}
          </div>

          <button
            onClick={handleUpload}
            disabled={!selectedFile || analyzeDocument.isPending}
            className="mt-6 w-full rounded-lg bg-amber-400 py-3 font-semibold text-gray-900 hover:bg-amber-500 transition disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {analyzeDocument.isPending ? (
              <span className="flex items-center justify-center gap-2">
                <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                Analyzing Document...
              </span>
            ) : (
              "Upload & Extract Information"
            )}
          </button>

          {analyzeDocument.isError && (
            <p className="mt-3 text-sm text-red-600 text-center">
              Failed to analyze document. Please try again.
            </p>
          )}
        </div>
      )}

      {step === "confirm" && extractedData && (
        <form onSubmit={handleSubmit} className="bg-white border border-gray-200 rounded-xl p-8">
          <div className="flex items-center gap-2 mb-6">
            <h2 className="text-lg font-semibold text-gray-900">Confirm Your Information</h2>
            <span className="text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded-full font-medium">
              Auto-filled from document
            </span>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Business Name</label>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-amber-400 focus:ring-1 focus:ring-amber-400 outline-none"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-amber-400 focus:ring-1 focus:ring-amber-400 outline-none"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Tax ID</label>
              <input
                type="text"
                value={taxId}
                onChange={(e) => setTaxId(e.target.value)}
                required
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-amber-400 focus:ring-1 focus:ring-amber-400 outline-none"
              />
            </div>
          </div>

          <hr className="my-6 border-gray-200" />
          <h3 className="text-sm font-semibold text-gray-900 mb-4">Business Address</h3>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Street</label>
              <input
                type="text"
                value={address.street}
                onChange={(e) => setAddress({ ...address, street: e.target.value })}
                required
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-amber-400 focus:ring-1 focus:ring-amber-400 outline-none"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Number</label>
              <input
                type="text"
                value={address.number}
                onChange={(e) => setAddress({ ...address, number: e.target.value })}
                required
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-amber-400 focus:ring-1 focus:ring-amber-400 outline-none"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Additional Info</label>
              <input
                type="text"
                value={address.additionalInformation}
                onChange={(e) => setAddress({ ...address, additionalInformation: e.target.value })}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-amber-400 focus:ring-1 focus:ring-amber-400 outline-none"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">ZIP Code</label>
              <input
                type="text"
                value={address.zipCode}
                onChange={(e) => setAddress({ ...address, zipCode: e.target.value })}
                required
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-amber-400 focus:ring-1 focus:ring-amber-400 outline-none"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">City</label>
              <input
                type="text"
                value={address.city}
                onChange={(e) => setAddress({ ...address, city: e.target.value })}
                required
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-amber-400 focus:ring-1 focus:ring-amber-400 outline-none"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">State</label>
              <input
                type="text"
                value={address.state}
                onChange={(e) => setAddress({ ...address, state: e.target.value })}
                required
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-amber-400 focus:ring-1 focus:ring-amber-400 outline-none"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Country</label>
              <input
                type="text"
                value={address.country}
                onChange={(e) => setAddress({ ...address, country: e.target.value })}
                required
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-amber-400 focus:ring-1 focus:ring-amber-400 outline-none"
              />
            </div>
          </div>

          <div className="flex gap-3 mt-8">
            <button
              type="button"
              onClick={() => setStep("upload")}
              className="flex-1 rounded-lg border border-gray-300 py-3 font-semibold text-gray-700 hover:bg-gray-50 transition"
            >
              ← Back
            </button>
            <button
              type="submit"
              disabled={registerSeller.isPending}
              className="flex-1 rounded-lg bg-amber-400 py-3 font-semibold text-gray-900 hover:bg-amber-500 transition disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {registerSeller.isPending ? "Submitting..." : "Confirm & Register"}
            </button>
          </div>

          {registerSeller.isError && (
            <p className="mt-3 text-sm text-red-600 text-center">
              Registration failed. Please try again.
            </p>
          )}
        </form>
      )}
    </div>
  );
}
