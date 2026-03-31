// Import the functions you need from the SDKs you need
import { initializeApp } from "firebase/app";
import { getAnalytics } from "firebase/analytics";
import { getFirestore } from "firebase/firestore";
import { getDatabase } from "firebase/database";

// Your web app's Firebase configuration
const firebaseConfig = {
  apiKey: "AIzaSyDAM8NhxCrs1j9l_Ip4lepUZ0u_B3aESjo",
  authDomain: "aurora-vortex-scalper-60b9c.firebaseapp.com",
  projectId: "aurora-vortex-scalper-60b9c",
  storageBucket: "aurora-vortex-scalper-60b9c.firebasestorage.app",
  messagingSenderId: "1068492017471",
  appId: "1:1068492017471:web:3b9c0f0f0cfdb6ac53cd8a",
  measurementId: "G-FXF47B6BP3"
};

// Initialize Firebase
const app = initializeApp(firebaseConfig);
const analytics = getAnalytics(app);
const db = getFirestore(app);
const rtdb = getDatabase(app);

// Make available globally if needed by plain JS scripts (like app.js)
window.firebaseApp = app;
window.firebaseDb = db;
window.firebaseRtdb = rtdb;

console.log("[FIREBASE] Web App Initialized successfully!");
