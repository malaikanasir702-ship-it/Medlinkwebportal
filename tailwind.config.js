/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        './Views/**/*.cshtml',
        './wwwroot/js/**/*.js'
    ],
    theme: {
        extend: {
            animation: {
                'float': 'float 6s ease-in-out infinite',
                'float-delayed': 'float 6s ease-in-out infinite 3s',
                'scan': 'scan 3s linear infinite',
                'bounce-slow': 'bounce-slow 4s ease-in-out infinite',
                'shimmer': 'shimmer 2s linear infinite',
                'spin-slow': 'spin 20s linear infinite',
                'spin-reverse': 'spin 10s linear infinite reverse',
                'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
                'ping-slow': 'ping 2s cubic-bezier(0, 0, 0.2, 1) infinite',
                'slide-in': 'slide-in 0.5s ease-out',
                'fade-in': 'fade-in 0.5s ease-out',
                'zoom-in': 'zoom-in 0.5s ease-out'
            },
            keyframes: {
                float: {
                    '0%, 100%': { transform: 'translateY(0) rotate(-6deg)' },
                    '50%': { transform: 'translateY(-30px) rotate(-4deg)' }
                },
                'float-delayed': {
                    '0%, 100%': { transform: 'translateY(0) rotate(6deg)' },
                    '50%': { transform: 'translateY(-30px) rotate(4deg)' }
                },
                scan: {
                    '0%': { top: '0%', opacity: '0' },
                    '10%': { opacity: '1' },
                    '90%': { opacity: '1' },
                    '100%': { top: '100%', opacity: '0' }
                },
                'bounce-slow': {
                    '0%, 100%': { transform: 'translateY(-50%) scale(1)' },
                    '50%': { transform: 'translateY(-60%) scale(1.1)' }
                },
                shimmer: {
                    '0%': { transform: 'translateX(-100%)' },
                    '100%': { transform: 'translateX(100%)' }
                },
                'slide-in': {
                    'from': { opacity: '0', transform: 'translateY(20px)' },
                    'to': { opacity: '1', transform: 'translateY(0)' }
                },
                'fade-in': {
                    'from': { opacity: '0' },
                    'to': { opacity: '1' }
                },
                'zoom-in': {
                    'from': { opacity: '0', transform: 'scale(0.95)' },
                    'to': { opacity: '1', transform: 'scale(1)' }
                }
            }
        },
    },
    plugins: [],
}
