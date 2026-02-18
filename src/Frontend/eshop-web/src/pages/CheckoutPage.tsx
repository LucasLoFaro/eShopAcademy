import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router";
import { useQueryClient } from "@tanstack/react-query";
import { useBasket } from "../hooks/useBasket";
import { usePlaceOrder } from "../hooks/useOrders";
import { useCustomer, useEnsureCustomer } from "../hooks/useCustomer";
import { useAddCustomerAddress, useDeleteCustomerAddress } from "../hooks/useCustomerAddresses";
import { useClientId } from "../hooks/useClientId";
import type { Address } from "../types";
import type { CheckoutData } from "../types";
import AddressStep from "../components/checkout/AddressStep";
import PaymentMethodStep from "../components/checkout/PaymentMethodStep";
import ConfirmationStep from "../components/checkout/ConfirmationStep";

type CheckoutStep = "address" | "payment" | "confirmation";

export default function CheckoutPage() {
const { data: basket, isLoading } = useBasket();
const { data: customer, isLoading: customerLoading } = useCustomer();
const ensureCustomer = useEnsureCustomer();
const placeOrder = usePlaceOrder();
const addAddress = useAddCustomerAddress();
const deleteAddress = useDeleteCustomerAddress();
const navigate = useNavigate();
const queryClient = useQueryClient();
const clientId = useClientId();

  const [currentStep, setCurrentStep] = useState<CheckoutStep>("address");
  const [checkoutData, setCheckoutData] = useState<CheckoutData>({});

  // Ensure customer exists on mount
  useEffect(() => {
    if (!customerLoading && !customer) {
      ensureCustomer.mutate();
    }
  }, [customerLoading, customer]);

  const items = basket?.items ?? [];
  const total = items.reduce((sum, i) => sum + i.product.price * i.quantity, 0);

  const handleAddressSubmit = async (address: Address, description?: string, saveAddress?: boolean) => {
    setCheckoutData((prev) => ({ ...prev, address }));
    
    // Save address if requested
    if (saveAddress && description) {
      try {
        await addAddress.mutateAsync({
          description,
          address,
          isDefault: false,
        });
      } catch (error) {
        console.error("Failed to save address:", error);
        // Continue anyway - address is saved in checkout state
      }
    }
    
    setCurrentStep("payment");
  };

  const handlePaymentSubmit = (paymentMethod: string) => {
    setCheckoutData((prev) => ({ ...prev, paymentMethod }));
    setCurrentStep("confirmation");
  };

  const handleDeleteAddress = async (addressId: string) => {
    await deleteAddress.mutateAsync(addressId);
  };

  const handlePlaceOrder = async () => {
    const cust = customer ?? ensureCustomer.data;
    if (!cust || !checkoutData.address) return;

    const result = await placeOrder.mutateAsync({
      customerId: cust.id,
      basketClientId: clientId,
      items: items.map((i) => ({
        productID: i.product.id,
        quantity: i.quantity,
        price: i.product.price,
      })),
      shippingAddress: {
        street: checkoutData.address.street,
        number: checkoutData.address.number,
        additionalInformation: checkoutData.address.additionalInformation || "",
        zipCode: checkoutData.address.zipCode,
        city: checkoutData.address.city,
      },
    });
    navigate(`/orders/${result.orderId}`, { state: { paymentUrl: result.paymentUrl } });
    queryClient.invalidateQueries({ queryKey: ["basket", clientId] });
  };

  if (isLoading || customerLoading) {
    return (
      <div className="flex justify-center py-12">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-amber-500" />
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="py-12 text-center">
        <p className="text-gray-500">Your basket is empty.</p>
        <Link to="/" className="mt-4 inline-block text-blue-700 hover:underline">
          Browse products
        </Link>
      </div>
    );
  }

  return (
    <>
      <h1 className="mb-6 text-2xl font-bold">Checkout</h1>

      {/* Progress Indicator */}
      <div className="mb-8 flex items-center justify-center">
        <div className="flex items-center gap-2">
          {/* Step 1: Address */}
          <div className="flex items-center">
            <div
              className={`flex h-10 w-10 items-center justify-center rounded-full font-semibold ${
                currentStep === "address"
                  ? "bg-amber-500 text-white"
                  : checkoutData.address
                  ? "bg-green-500 text-white"
                  : "bg-gray-200 text-gray-600"
              }`}
            >
              {checkoutData.address && currentStep !== "address" ? "?" : "1"}
            </div>
            <span
              className={`ml-2 text-sm font-medium ${
                currentStep === "address" ? "text-gray-900" : "text-gray-500"
              }`}
            >
              Address
            </span>
          </div>

          <div className="mx-4 h-0.5 w-12 bg-gray-300" />

          {/* Step 2: Payment */}
          <div className="flex items-center">
            <div
              className={`flex h-10 w-10 items-center justify-center rounded-full font-semibold ${
                currentStep === "payment"
                  ? "bg-amber-500 text-white"
                  : checkoutData.paymentMethod
                  ? "bg-green-500 text-white"
                  : "bg-gray-200 text-gray-600"
              }`}
            >
              {checkoutData.paymentMethod && currentStep === "confirmation" ? "?" : "2"}
            </div>
            <span
              className={`ml-2 text-sm font-medium ${
                currentStep === "payment" ? "text-gray-900" : "text-gray-500"
              }`}
            >
              Payment
            </span>
          </div>

          <div className="mx-4 h-0.5 w-12 bg-gray-300" />

          {/* Step 3: Confirmation */}
          <div className="flex items-center">
            <div
              className={`flex h-10 w-10 items-center justify-center rounded-full font-semibold ${
                currentStep === "confirmation"
                  ? "bg-amber-500 text-white"
                  : "bg-gray-200 text-gray-600"
              }`}
            >
              3
            </div>
            <span
              className={`ml-2 text-sm font-medium ${
                currentStep === "confirmation" ? "text-gray-900" : "text-gray-500"
              }`}
            >
              Review
            </span>
          </div>
        </div>
      </div>

      {/* Step Content */}
      <div className="mb-8">
        {currentStep === "address" && (
          <AddressStep
            address={checkoutData.address}
            savedAddresses={customer?.savedAddresses ?? []}
            onNext={handleAddressSubmit}
            onDeleteAddress={handleDeleteAddress}
          />
        )}

        {currentStep === "payment" && (
          <PaymentMethodStep
            selectedMethod={checkoutData.paymentMethod}
            onNext={handlePaymentSubmit}
            onBack={() => setCurrentStep("address")}
          />
        )}

        {currentStep === "confirmation" && checkoutData.address && checkoutData.paymentMethod && (
          <ConfirmationStep
            address={checkoutData.address}
            paymentMethod={checkoutData.paymentMethod}
            items={items}
            total={total}
            isProcessing={placeOrder.isPending}
            error={placeOrder.isError ? placeOrder.error.message || "Failed to place order." : undefined}
            onConfirm={handlePlaceOrder}
            onBack={() => setCurrentStep("payment")}
          />
        )}
      </div>
    </>
  );
}

