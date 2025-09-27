import React from "react";
import ReactDOM from "react-dom/client";
import "./index.css";
import { Game } from "./Pages/Game/Game";
import reportWebVitals from "./reportWebVitals";
import { Route, BrowserRouter as Router, Routes } from "react-router-dom";
import { SocketContextProvider } from "./Context/SocketContext";
import { PlayerInfo } from "./Pages/PlayerInfo/PlayerInfo";

const root = ReactDOM.createRoot(
  document.getElementById("root") as HTMLElement
);
root.render(
  <Router>
    <SocketContextProvider>
      <Routes>
        <Route path="/" element={<PlayerInfo />} />
        <Route path="/game" element={<Game />} />
      </Routes>
    </SocketContextProvider>
  </Router>
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();
