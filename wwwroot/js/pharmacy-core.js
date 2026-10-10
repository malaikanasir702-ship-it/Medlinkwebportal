/**
 * Pharmacy Core - Handles Medicine Ordering, Checkout, and Tracking
 */

const Pharmacy = {
    context: null,
    items: [],

    init: function (ctx) {
        this.context = ctx;
        console.log("Pharmacy: Initialized");
    },

    placeOrder: async function (orderData) {
        const address = document.getElementById("deliveryAddress").value.trim();
        if (!address) {
            alert("Please provide a delivery address.");
            return;
        }

        const btn = document.getElementById("btnPlaceOrder");
        const originalText = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';

        const paymentMethod = document.querySelector('input[name="paymentMethod"]:checked').value;

        try {
            const res = await fetch('/Pharmacy/PlaceOrder', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    appointmentId: orderData.appointmentId,
                    prescriptionId: orderData.prescriptionId,
                    shippingAddress: address,
                    paymentMethod: parseInt(paymentMethod),
                    items: orderData.items
                })
            });

            const data = await res.json();
            if (data.success) {
                // Success state with animation
                btn.innerHTML = '<i class="fas fa-check"></i> Success!';
                btn.classList.remove('bg-emerald-600');
                btn.classList.add('bg-emerald-500');
                setTimeout(() => {
                    window.location.href = `/Pharmacy/OrderTracking/${data.orderId}`;
                }, 1000);
            } else {
                alert("Order Failed: " + data.message);
                btn.disabled = false;
                btn.innerHTML = originalText;
            }
        } catch (e) {
            console.error(e);
            alert("Network Error");
            btn.disabled = false;
            btn.innerHTML = originalText;
        }
    }
};

window.Pharmacy = Pharmacy;
