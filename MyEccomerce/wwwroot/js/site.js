import { initializeApp, getApps, getApp } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-app.js";
import { getMessaging, getToken } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-messaging.js";

// Ang imong saktong Firebase Configuration
const firebaseConfig = {
    apiKey: "AIzaSyCv7kIoegSxHQjNnkR1MtEVpYExo9lc_q4",
    authDomain: "myeccomerce-3ef7d.firebaseapp.com",
    projectId: "myeccomerce-3ef7d",
    storageBucket: "myeccomerce-3ef7d.firebasestorage.app",
    messagingSenderId: "642933203986",
    appId: "1:642933203986:web:e7900f3983aa817de0e4cb",
    measurementId: "G-KZTJJYLNK1"
};

// 💡 SULBAD SA DUPLICATE APP: I-tsek kung na-initialize na ba ang Firebase kaniadto
const app = getApps().length === 0 ? initializeApp(firebaseConfig) : getApp();
const messaging = getMessaging(app);

if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/firebase-messaging-sw.js', { type: 'module' })
        .then((registration) => {
            console.log('Service Worker registered successfully:', registration);

            Notification.requestPermission().then((permission) => {
                if (permission === 'granted') {
                    console.log('Notification permission granted.');

                    getToken(messaging, {
                        serviceWorkerRegistration: registration,
                        vapidKey: 'BNywPFMtkgP9FLMoi4VF1iLe8RQu3TuyJdjnsK2ozZPLtyvnt002pwuDIbNfyQ0KTcM13D4q2xJQtgCTcOxOpIc'
                    })
                        .then((currentToken) => {
                            if (currentToken) {
                                console.log("Device Token:", currentToken);
                                sendTokenToServer(currentToken);
                            } else {
                                console.log('No registration token available.');
                            }
                        }).catch((err) => {
                            console.log('Error retrieving token: ', err);
                        });
                } else {
                    console.log('Notification permission denied.');
                }
            });
        }).catch((err) => {
            console.log('Service Worker registration failed: ', err);
        });
}

function sendTokenToServer(token) {
    fetch('/PushNotification/SaveToken', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: 'token=' + encodeURIComponent(token)
    })
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.json();
        })
        .then(data => console.log('Token saved to server status:', data))
        .catch(error => console.error('Error syncing token to server:', error));
}