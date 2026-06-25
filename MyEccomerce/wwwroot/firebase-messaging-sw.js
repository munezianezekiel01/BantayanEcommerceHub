import { initializeApp } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-app.js";
import { getMessaging, onBackgroundMessage } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-messaging-sw.js";

const firebaseConfig = {
    apiKey: "AIzaSyCv7kIoegSxHQjNnkR1MtEVpYExo9lc_q4",
    authDomain: "myeccomerce-3ef7d.firebaseapp.com",
    projectId: "myeccomerce-3ef7d",
    storageBucket: "myeccomerce-3ef7d.firebasestorage.app",
    messagingSenderId: "642933203986",
    appId: "1:642933203986:web:e7900f3983aa817de0e4cb"
};

const app = initializeApp(firebaseConfig);
const messaging = getMessaging(app);

// Nadawat nga mensahe kung SIRADO ang browser o wala gi-focus ang tab
onBackgroundMessage(messaging, (payload) => {
    console.log("Nadawat nga mensahe sa background: ", payload);

    // 💡 Migamit og '?.' aron kung walay notification object, dili mo-crash ang script
    const notificationTitle = payload.notification?.title || "B-HUB Update";

    const notificationOptions = {
        body: payload.notification?.body || "Naay bag-ong update sa imong order, boss!",
        icon: '/images/notification-icon.png', // Siguroha nga naa kini nga file sa imong wwwroot/images
        badge: '/images/badge.png', // Gamay nga icon para sa status bar (optional)
        data: {
            // 💡 Dinhi makuha ang link nga gipasa gikan sa C# Controller ($"/Orders/OrderDetails/{orderId}")
            url: payload.data?.targetUrl || '/'
        }
    };

    // Mupakita na sa lumad nga snackbar/notification banner sa OS
    self.registration.showNotification(notificationTitle, notificationOptions);
});

// 💡 I-DUGANG KINI: Aron inig click sa user sa notification banner, mo-abli ang saktong page sa B-HUB
self.addEventListener('notificationclick', function (event) {
    event.notification.close(); // Sirad-an ang banner

    const targetUrl = event.notification.data?.url || '/';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (clientList) {
            // Kung abli na daan ang B-HUB tab, i-focus lang kini
            for (let i = 0; i < clientList.length; i++) {
                let client = clientList[i];
                if (client.url.includes(targetUrl) && 'focus' in client) {
                    return client.focus();
                }
            }
            // Kung sirado ang tab, mo-abli og bag-ong window padulong sa OrderDetails
            if (clients.openWindow) {
                return clients.openWindow(targetUrl);
            }
        })
    );
});