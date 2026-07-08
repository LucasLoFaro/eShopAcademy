declare module "eshopSellers/SellerDashboard" {
  import type { ComponentType } from "react";

  interface SellerDashboardProps {
    token: string | null;
  }

  const SellerDashboard: ComponentType<SellerDashboardProps>;
  export default SellerDashboard;
}
