// Template: Optimizely/Episerver Commerce 13 cart + pricing service — EPiServer.Commerce 13.x on .NET Framework 4.x
// Packages: EPiServer.Commerce.Core 13.x (net461). Order abstractions in EPiServer.Commerce.Order;
//           legacy foundation in Mediachase.Commerce.*.
// Placeholders: {{Namespace}}
//
// Carts/orders are loaded and saved through IOrderRepository; line items/shipments via the cart.
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Pricing;
using EPiServer.Commerce.Catalog.ContentTypes;
using System.Collections.Generic;

namespace {{Namespace}}.Commerce
{
    public class CartService
    {
        private const string DefaultCartName = "Default";

        private readonly IOrderRepository _orderRepository;
        private readonly IPriceDetailService _priceDetailService;

        public CartService(IOrderRepository orderRepository, IPriceDetailService priceDetailService)
        {
            _orderRepository = orderRepository;
            _priceDetailService = priceDetailService;
        }

        public ICart GetOrCreateCart(System.Guid contactId) =>
            _orderRepository.LoadOrCreateCart<ICart>(contactId, DefaultCartName);

        public void AddItem(ICart cart, string variationCode, decimal quantity)
        {
            var shipment = cart.GetFirstShipment();
            var lineItem = cart.CreateLineItem(variationCode);   // via IOrderGroupFactory
            lineItem.Quantity = quantity;
            shipment.LineItems.Add(lineItem);

            _orderRepository.Save(cart);   // persist the mutated cart
        }

        public IPurchaseOrder Checkout(ICart cart)
        {
            // Validate/recalculate before checkout in real code (IOrderGroupCalculator, validation).
            var orderReference = _orderRepository.SaveAsPurchaseOrder(cart);
            _orderRepository.Delete(cart.OrderLink);   // clear the cart after conversion
            return _orderRepository.Load<IPurchaseOrder>(orderReference.OrderGroupId);
        }

        public IEnumerable<IPriceDetailValue> ListPrices(VariationContent variation) =>
            _priceDetailService.List(variation.ContentLink);
    }
}
