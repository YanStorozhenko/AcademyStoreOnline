// Функции для работы с корзиной
class CartManager {
    constructor() {
        this.apiBaseUrl = '/api/cart';
        this.initializeCart();
    }
    
    initializeCart() {
        this.updateCartCount();
        this.setupCartEventListeners();
    }
    
    async updateCartCount() {
        try {
            const response = await fetch(`${this.apiBaseUrl}/count`);
            if (response.ok) {
                const count = await response.text();
                this.updateCartBadge(count);
            }
        } catch (error) {
            console.error('Error updating cart count:', error);
        }
    }
    
    updateCartBadge(count) {
        // Обновляем бейджи в десктопной и мобильной навигации
        const badges = document.querySelectorAll('.cart-count');
        badges.forEach(badge => {
            badge.textContent = count;
            badge.style.display = count > 0 ? 'flex' : 'none';
        });
    }
    
    setupCartEventListeners() {
        // Обработчики для кнопок "В корзину" на страницах товаров
        document.querySelectorAll('.add-to-cart-btn').forEach(button => {
            button.addEventListener('click', async (e) => {
                e.preventDefault();
                
                const form = button.closest('form');
                const productId = form.querySelector('input[name="productId"]').value;
                
                await this.addToCart(productId);
            });
        });
        
        // Обработчики для кнопок изменения количества в корзине
        document.querySelectorAll('.quantity-btn').forEach(button => {
            button.addEventListener('click', async (e) => {
                e.preventDefault();
                
                const form = button.closest('form');
                const cartItemId = form.querySelector('input[name="cartItemId"]').value;
                const change = parseInt(form.querySelector('input[name="change"]').value);
                
                await this.updateQuantity(cartItemId, change);
            });
        });
    }
    
    async addToCart(productId, quantity = 1) {
        try {
            const response = await fetch(`${this.apiBaseUrl}/add/${productId}?quantity=${quantity}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            
            if (response.ok) {
                const result = await response.json();
                this.showToast('success', result.message);
                this.updateCartCount();
            } else {
                this.showToast('error', 'Ошибка при добавлении товара');
            }
        } catch (error) {
            console.error('Error adding to cart:', error);
            this.showToast('error', 'Ошибка при добавлении товара');
        }
    }
    
    async updateQuantity(cartItemId, change) {
        try {
            const form = document.querySelector(`form[data-cart-item-id="${cartItemId}"]`);
            const newQuantity = parseInt(form.querySelector('.quantity-display').textContent) + change;
            
            if (newQuantity < 1 || newQuantity > 10) return;
            
            // Отправляем запрос на обновление
            form.submit();
        } catch (error) {
            console.error('Error updating quantity:', error);
        }
    }
    
    showToast(type, message) {
        // Создаем тост уведомление
        const toastContainer = document.getElementById('toast-container') || this.createToastContainer();
        
        const toast = document.createElement('div');
        toast.className = `toast show ${type === 'success' ? 'bg-success' : 'bg-danger'} text-white`;
        toast.setAttribute('role', 'alert');
        toast.innerHTML = `
            <div class="toast-body d-flex justify-content-between align-items-center">
                <span>${message}</span>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
            </div>
        `;
        
        toastContainer.appendChild(toast);
        
        // Автоматическое удаление через 3 секунды
        setTimeout(() => {
            toast.remove();
        }, 3000);
    }
    
    createToastContainer() {
        const container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(container);
        return container;
    }
}

// Инициализация при загрузке страницы
document.addEventListener('DOMContentLoaded', () => {
    const cartManager = new CartManager();
    
    // Экспортируем для использования в консоли
    window.cartManager = cartManager;
});