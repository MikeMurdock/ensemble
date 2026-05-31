// =============================================================================
// Example: Optimizely/Episerver Commerce 13 cart -> checkout slice
// -----------------------------------------------------------------------------
// Shows the order abstractions connecting: catalog variation -> cart line item ->
// pricing -> purchase order. EPiServer.Commerce 13.x on .NET Framework 4.x.
// Genericized (Acme). Illustrative single file.
//
// Order abstractions: EPiServer.Commerce.Order. Legacy foundation: Mediachase.Commerce.*.
// Platform layer (csproj/web.config/binding redirects) -> developing-with-dotnet-4x.
// =============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Pricing;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace Acme.Commerce.Example
{
    // ---- A catalog variation (the buyable SKU) -------------------------------
    public class AcmeVariation : VariationContent
    {
        public virtual string Color { get; set; }
    }

    // ---- A cart/checkout service over IOrderRepository -----------------------
    public interface ICheckoutService
    {
        ICart GetOrCreateCart(Guid contactId);
        void AddVariation(ICart cart, string variationCode, decimal quantity);
        decimal GetUnitPrice(VariationContent variation);
        IPurchaseOrder PlaceOrder(ICart cart);
    }

    public class CheckoutService : ICheckoutService
    {
        private const string CartName = "Default";

        private readonly IOrderRepository _orderRepository;
        private readonly IPriceDetailService _priceDetailService;
        private readonly IContentLoader _contentLoader;

        public CheckoutService(
            IOrderRepository orderRepository,
            IPriceDetailService priceDetailService,
            IContentLoader contentLoader)
        {
            _orderRepository = orderRepository;
            _priceDetailService = priceDetailService;
            _contentLoader = contentLoader;
        }

        public ICart GetOrCreateCart(Guid contactId) =>
            _orderRepository.LoadOrCreateCart<ICart>(contactId, CartName);

        public void AddVariation(ICart cart, string variationCode, decimal quantity)
        {
            var shipment = cart.GetFirstShipment();
            var line = cart.CreateLineItem(variationCode);   // IOrderGroupFactory under the hood
            line.Quantity = quantity;
            shipment.LineItems.Add(line);

            _orderRepository.Save(cart);   // persist the mutated cart
        }

        public decimal GetUnitPrice(VariationContent variation)
        {
            // Prices are market/currency-scoped; a missing market is a common "no price" cause.
            var prices = _priceDetailService.List(variation.ContentLink);
            return prices.Select(p => p.UnitPrice.Amount).DefaultIfEmpty(0m).Min();
        }

        public IPurchaseOrder PlaceOrder(ICart cart)
        {
            // Real checkout: validate line items, run IPromotionEngine, recalculate totals first.
            var orderRef = _orderRepository.SaveAsPurchaseOrder(cart);
            _orderRepository.Delete(cart.OrderLink);   // clear cart after conversion
            return _orderRepository.Load<IPurchaseOrder>(orderRef.OrderGroupId);
        }
    }

    // ---- Usage sketch --------------------------------------------------------
    public static class CheckoutFlow
    {
        public static IPurchaseOrder Run(ICheckoutService checkout, Guid contactId, string sku)
        {
            var cart = checkout.GetOrCreateCart(contactId);
            checkout.AddVariation(cart, sku, quantity: 1);
            return checkout.PlaceOrder(cart);
        }
    }
}
