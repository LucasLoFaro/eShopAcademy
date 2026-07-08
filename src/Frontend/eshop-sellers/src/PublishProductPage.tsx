import { useState, useRef } from "react";
import { usePublishProduct, useUpdateProduct, useUploadProductImage, useCategories, type SellerProduct } from "./hooks/useProducts";

interface ProductSpec {
  label: string;
  value: string;
}

interface ProductFaq {
  question: string;
  answer: string;
}

interface PublishProductPageProps {
  onBack: () => void;
  editProduct?: SellerProduct | null;
}

export default function PublishProductPage({ onBack, editProduct }: PublishProductPageProps) {
  const isEditing = !!editProduct;
  const [name, setName] = useState(editProduct?.name ?? "");
  const [price, setPrice] = useState(editProduct?.price?.toString() ?? "");
  const [description, setDescription] = useState(editProduct?.description ?? "");
  const [categoryId, setCategoryId] = useState(editProduct?.category?.id ?? "");
  const [aboutHtml, setAboutHtml] = useState(editProduct?.aboutHtml ?? "");
  const [images, setImages] = useState<string[]>(() => {
    if (!editProduct) return [];
    const imgs: string[] = [];
    if (editProduct.imageUrl) imgs.push(editProduct.imageUrl);
    if (editProduct.additionalImages) imgs.push(...editProduct.additionalImages);
    return imgs;
  });
  const [uploading, setUploading] = useState(false);
  const [specs, setSpecs] = useState<ProductSpec[]>(
    editProduct?.specs?.length ? editProduct.specs : [{ label: "", value: "" }]
  );
  const [faqs, setFaqs] = useState<ProductFaq[]>(
    editProduct?.faqs?.length ? editProduct.faqs : [{ question: "", answer: "" }]
  );
  const dragItem = useRef<number | null>(null);
  const dragOverItem = useRef<number | null>(null);

  const uploadImage = useUploadProductImage();
  const publishProduct = usePublishProduct();
  const updateProduct = useUpdateProduct();
  const { data: categories, isLoading: loadingCategories } = useCategories();

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      const url = await uploadImage.mutateAsync(file);
      setImages((prev) => [...prev, url]);
    } finally {
      setUploading(false);
      e.target.value = "";
    }
  };

  const handleDragStart = (index: number) => {
    dragItem.current = index;
  };

  const handleDragEnter = (index: number) => {
    dragOverItem.current = index;
  };

  const handleDragEnd = () => {
    if (dragItem.current === null || dragOverItem.current === null) return;
    const reordered = [...images];
    const [dragged] = reordered.splice(dragItem.current, 1);
    reordered.splice(dragOverItem.current, 0, dragged);
    setImages(reordered);
    dragItem.current = null;
    dragOverItem.current = null;
  };

  const removeImage = (index: number) => {
    setImages((prev) => prev.filter((_, i) => i !== index));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (images.length === 0) {
      alert("Please upload at least one product image.");
      return;
    }

    const payload = {
      name,
      price: parseFloat(price),
      description,
      imageUrl: images[0],
      categoryId,
      additionalImages: images.slice(1),
      aboutHtml,
      specs: specs.filter((s) => s.label && s.value),
      faqs: faqs.filter((f) => f.question && f.answer),
    };

    if (isEditing && editProduct) {
      await updateProduct.mutateAsync({
        ...payload,
        id: editProduct.id,
        sellerId: editProduct.sellerId ?? "",
      });
    } else {
      await publishProduct.mutateAsync(payload);
    }
  };

  if (publishProduct.isSuccess || updateProduct.isSuccess) {
    return (
      <div className="p-8 text-center">
        <div className="text-5xl mb-4">🎉</div>
        <h2 className="text-2xl font-bold text-gray-900 mb-2">
          {isEditing ? "Product Updated!" : "Product Published!"}
        </h2>
        <p className="text-gray-600 mb-6">
          {isEditing ? "Your product has been updated." : "Your product is now live on eShop Academy."}
        </p>
        <button
          onClick={onBack}
          className="rounded-lg bg-indigo-600 px-6 py-3 font-semibold text-white hover:bg-indigo-700 transition"
        >
          Back to Dashboard
        </button>
      </div>
    );
  }

  return (
    <div className="py-8 px-4 max-w-3xl mx-auto">
      <div className="flex items-center gap-4 mb-8">
        <button onClick={onBack} className="text-gray-500 hover:text-gray-700">
          ← Back
        </button>
        <h1 className="text-2xl font-bold text-gray-900">{isEditing ? "Edit Product" : "Publish a Product"}</h1>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Basic Info */}
        <div className="bg-white border border-gray-200 rounded-xl p-6 space-y-4">
          <h2 className="text-lg font-semibold text-gray-900">Basic Information</h2>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Product Name *</label>
            <input
              type="text"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="w-full rounded-lg border border-gray-300 px-4 py-2 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
              placeholder="e.g., Wireless Bluetooth Headphones"
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Price *</label>
              <input
                type="number"
                required
                step="0.01"
                min="0.01"
                value={price}
                onChange={(e) => setPrice(e.target.value)}
                className="w-full rounded-lg border border-gray-300 px-4 py-2 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                placeholder="29.99"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Category *</label>
              <select
                required
                value={categoryId}
                onChange={(e) => setCategoryId(e.target.value)}
                className="w-full rounded-lg border border-gray-300 px-4 py-2 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
              >
                <option value="">Select a category</option>
                {loadingCategories ? (
                  <option disabled>Loading...</option>
                ) : (
                  categories?.map((cat) => (
                    <option key={cat.id} value={cat.id}>{cat.name}</option>
                  ))
                )}
              </select>
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Short Description *</label>
            <textarea
              required
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full rounded-lg border border-gray-300 px-4 py-2 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
              placeholder="Brief product description..."
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">About (HTML)</label>
            <textarea
              rows={4}
              value={aboutHtml}
              onChange={(e) => setAboutHtml(e.target.value)}
              className="w-full rounded-lg border border-gray-300 px-4 py-2 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 font-mono text-sm"
              placeholder="<p>Detailed product description with HTML formatting...</p>"
            />
          </div>
        </div>

        {/* Images */}
        <div className="bg-white border border-gray-200 rounded-xl p-6 space-y-4">
          <h2 className="text-lg font-semibold text-gray-900">Images</h2>
          <p className="text-sm text-gray-500">
            The first image is the main product image. Drag and drop to reorder.
          </p>

          <input
            type="file"
            accept="image/jpeg,image/png,image/webp,image/gif"
            onChange={handleImageUpload}
            disabled={uploading}
            className="text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-indigo-50 file:text-indigo-700 hover:file:bg-indigo-100 disabled:opacity-50"
          />
          {uploading && <p className="text-sm text-gray-500 mt-1">Uploading...</p>}

          {images.length > 0 && (
            <div className="flex gap-3 mt-3 flex-wrap">
              {images.map((url, i) => (
                <div
                  key={url}
                  draggable
                  onDragStart={() => handleDragStart(i)}
                  onDragEnter={() => handleDragEnter(i)}
                  onDragEnd={handleDragEnd}
                  onDragOver={(e) => e.preventDefault()}
                  className={`relative group cursor-grab active:cursor-grabbing border-2 rounded-lg p-1 transition ${
                    i === 0 ? "border-indigo-500 ring-2 ring-indigo-200" : "border-gray-200 hover:border-gray-300"
                  }`}
                >
                  <img
                    src={url}
                    alt={`Product ${i + 1}`}
                    className="h-24 w-24 rounded object-cover"
                  />
                  {i === 0 && (
                    <span className="absolute -top-2 -left-2 bg-indigo-600 text-white text-xs px-1.5 py-0.5 rounded font-medium">
                      Main
                    </span>
                  )}
                  <button
                    type="button"
                    onClick={() => removeImage(i)}
                    className="absolute -top-2 -right-2 bg-red-500 text-white rounded-full w-5 h-5 text-xs flex items-center justify-center opacity-0 group-hover:opacity-100 transition"
                  >
                    ×
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Specifications */}
        <div className="bg-white border border-gray-200 rounded-xl p-6 space-y-4">
          <h2 className="text-lg font-semibold text-gray-900">Specifications</h2>
          {specs.map((spec, i) => (
            <div key={i} className="flex gap-3 items-center">
              <input
                type="text"
                value={spec.label}
                onChange={(e) => {
                  const updated = [...specs];
                  updated[i].label = e.target.value;
                  setSpecs(updated);
                }}
                placeholder="Label (e.g., Weight)"
                className="flex-1 rounded-lg border border-gray-300 px-3 py-2 text-sm"
              />
              <input
                type="text"
                value={spec.value}
                onChange={(e) => {
                  const updated = [...specs];
                  updated[i].value = e.target.value;
                  setSpecs(updated);
                }}
                placeholder="Value (e.g., 250g)"
                className="flex-1 rounded-lg border border-gray-300 px-3 py-2 text-sm"
              />
              <button
                type="button"
                onClick={() => setSpecs(specs.filter((_, idx) => idx !== i))}
                className="text-red-400 hover:text-red-600 text-lg"
              >
                ×
              </button>
            </div>
          ))}
          <button
            type="button"
            onClick={() => setSpecs([...specs, { label: "", value: "" }])}
            className="text-sm text-indigo-600 hover:text-indigo-700 font-medium"
          >
            + Add Specification
          </button>
        </div>

        {/* FAQs */}
        <div className="bg-white border border-gray-200 rounded-xl p-6 space-y-4">
          <h2 className="text-lg font-semibold text-gray-900">FAQs</h2>
          {faqs.map((faq, i) => (
            <div key={i} className="space-y-2 border-b border-gray-100 pb-3 last:border-0">
              <div className="flex gap-3 items-center">
                <input
                  type="text"
                  value={faq.question}
                  onChange={(e) => {
                    const updated = [...faqs];
                    updated[i].question = e.target.value;
                    setFaqs(updated);
                  }}
                  placeholder="Question"
                  className="flex-1 rounded-lg border border-gray-300 px-3 py-2 text-sm"
                />
                <button
                  type="button"
                  onClick={() => setFaqs(faqs.filter((_, idx) => idx !== i))}
                  className="text-red-400 hover:text-red-600 text-lg"
                >
                  ×
                </button>
              </div>
              <textarea
                value={faq.answer}
                onChange={(e) => {
                  const updated = [...faqs];
                  updated[i].answer = e.target.value;
                  setFaqs(updated);
                }}
                placeholder="Answer"
                rows={2}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm"
              />
            </div>
          ))}
          <button
            type="button"
            onClick={() => setFaqs([...faqs, { question: "", answer: "" }])}
            className="text-sm text-indigo-600 hover:text-indigo-700 font-medium"
          >
            + Add FAQ
          </button>
        </div>

        {/* Submit */}
        <div className="flex justify-end">
          <button
            type="submit"
            disabled={publishProduct.isPending || updateProduct.isPending || !name || !price || !categoryId || !description || images.length === 0}
            className="rounded-lg bg-indigo-600 px-8 py-3 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition"
          >
            {publishProduct.isPending || updateProduct.isPending
              ? (isEditing ? "Saving..." : "Publishing...")
              : (isEditing ? "Save Changes" : "Publish Product")}
          </button>
        </div>

        {(publishProduct.isError || updateProduct.isError) && (
          <p className="text-red-600 text-sm text-center">
            {isEditing ? "Failed to update product." : "Failed to publish product."} Please try again.
          </p>
        )}
      </form>
    </div>
  );
}
