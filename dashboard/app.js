// ============================================================
//   AURORA VORTEX SCALPER — Dashboard JavaScript Engine
// ============================================================

// ── State ─────────────────────────────────────────────────
const state = {
  running: true,
  totalProfit: 0,
  dailyLoss: 0,
  tradeCount: 0,
  winCount: 0,
  activeTrades: {},
  profitHistory: [0],
  settings: {
    tradeSize: 10,
    takeProfit: 0.8,
    stopLoss: 1.5,
    maxLoss: 5,
    spikeThresh: 1.5,
    notifications: true
  }
};

// ── Pairs & Price simulation ───────────────────────────────
const PAIRS = [
  { sym: 'SOL/USDT',  price: 142.50, vol: 1.0 },
  { sym: 'ETH/USDT',  price: 3320.00, vol: 1.0 },
  { sym: 'BTC/USDT',  price: 68200.00, vol: 1.0 },
  { sym: 'BNB/USDT',  price: 584.20, vol: 1.0 },
  { sym: 'ARB/USDT',  price: 0.9810, vol: 1.0 },
  { sym: 'OP/USDT',   price: 2.4430, vol: 1.0 },
  { sym: 'AVAX/USDT', price: 36.70, vol: 1.0 },
  { sym: 'LINK/USDT', price: 15.62, vol: 1.0 },
  { sym: 'PEPE/USDT', price: 0.000012, vol: 1.0 },
  { sym: 'SHIB/USDT', price: 0.0000248, vol: 1.0 },
];

// ── Chart Setup ────────────────────────────────────────────
const ctx = document.getElementById('profit-chart').getContext('2d');

// Simple inline chart (no external lib dependency)
let profitData = [0];
const MAX_POINTS = 40;

function drawChart() {
  const canvas = document.getElementById('profit-chart');
  const w = canvas.width = canvas.offsetWidth * window.devicePixelRatio;
  const h = canvas.height = 160 * window.devicePixelRatio;
  const c = ctx;
  c.scale(window.devicePixelRatio, window.devicePixelRatio);
  const W = canvas.offsetWidth, H = 160;

  c.clearRect(0, 0, W, H);

  const data = profitData;
  if (data.length < 2) return;

  const minV = Math.min(...data) - 0.1;
  const maxV = Math.max(...data) + 0.1;
  const range = maxV - minV || 1;

  const stepX = W / (MAX_POINTS - 1);
  const toY = v => H - 12 - ((v - minV) / range) * (H - 24);

  // Grid lines
  c.strokeStyle = 'rgba(255,255,255,0.05)';
  c.lineWidth = 1;
  for (let i = 0; i <= 4; i++) {
    const y = 12 + (i / 4) * (H - 24);
    c.beginPath(); c.moveTo(0, y); c.lineTo(W, y); c.stroke();
  }

  // Gradient fill
  const grad = c.createLinearGradient(0, 0, 0, H);
  grad.addColorStop(0, 'rgba(124,58,237,0.35)');
  grad.addColorStop(1, 'rgba(124,58,237,0)');

  c.beginPath();
  c.moveTo(0, toY(data[0]));
  data.forEach((v, i) => {
    const x = i * (W / (data.length - 1));
    c.lineTo(x, toY(v));
  });
  c.lineTo(W, H); c.lineTo(0, H); c.closePath();
  c.fillStyle = grad;
  c.fill();

  // Line
  c.beginPath();
  c.moveTo(0, toY(data[0]));
  data.forEach((v, i) => {
    const x = i * (W / (data.length - 1));
    c.lineTo(x, toY(v));
  });
  c.strokeStyle = '#7c3aed';
  c.lineWidth = 2.5;
  c.lineJoin = 'round';
  c.stroke();

  // Dot at end
  const lastX = (data.length - 1) * (W / (data.length - 1));
  const lastY = toY(data[data.length - 1]);
  c.beginPath();
  c.arc(lastX, lastY, 4, 0, Math.PI * 2);
  c.fillStyle = '#7c3aed';
  c.fill();
  c.beginPath();
  c.arc(lastX, lastY, 7, 0, Math.PI * 2);
  c.fillStyle = 'rgba(124,58,237,0.3)';
  c.fill();

  // Zero line label
  const zeroY = toY(0);
  c.strokeStyle = 'rgba(255,255,255,0.1)';
  c.setLineDash([4, 4]);
  c.beginPath(); c.moveTo(0, zeroY); c.lineTo(W, zeroY); c.stroke();
  c.setLineDash([]);

  // Scale back
  c.scale(1 / window.devicePixelRatio, 1 / window.devicePixelRatio);
}

// ── Radar ─────────────────────────────────────────────────
function buildRadar() {
  const grid = document.getElementById('radar-grid');
  grid.innerHTML = PAIRS.map(p => `
    <div class="radar-item" id="radar-${p.sym.replace('/', '')}">
      <div>
        <div class="radar-sym">${p.sym}</div>
        <div class="radar-change up" id="chg-${p.sym.replace('/', '')}">+0.00%</div>
      </div>
      <div class="radar-price" id="px-${p.sym.replace('/', '')}">${fmt(p.price)}</div>
    </div>
  `).join('');
}

function fmt(n) {
  if (n < 0.01) return '$' + n.toFixed(8);
  if (n < 1)   return '$' + n.toFixed(4);
  if (n < 100) return '$' + n.toFixed(3);
  return '$' + n.toLocaleString('en-US', { maximumFractionDigits: 2 });
}

// ── Simulator ─────────────────────────────────────────────
function simulateTick() {
  if (!state.running) return;

  PAIRS.forEach(p => {
    // Random price movement ±0.2%
    const delta = (Math.random() - 0.48) * 0.004;
    const oldPrice = p.price;
    p.price = p.price * (1 + delta);
    const changePct = (p.price - oldPrice) / oldPrice;

    // Volume spike simulation (random 2% chance)
    const isSpike = Math.random() < 0.02;
    if (isSpike) p.vol = p.vol * (1 + (Math.random() * 1.5 + state.settings.spikeThresh - 1));
    else p.vol = p.vol * (0.95 + Math.random() * 0.1);

    // Update DOM
    const id = p.sym.replace('/', '');
    const el = document.getElementById(`radar-${id}`);
    const pxEl = document.getElementById(`px-${id}`);
    const chgEl = document.getElementById(`chg-${id}`);
    if (!el) return;

    pxEl.textContent = fmt(p.price);
    const pct = (changePct * 100).toFixed(3);
    chgEl.textContent = (changePct >= 0 ? '+' : '') + pct + '%';
    chgEl.className = 'radar-change ' + (changePct >= 0 ? 'up' : 'down');

    if (isSpike) {
      el.classList.add('spike');
      setTimeout(() => el.classList.remove('spike'), 1500);
      addLog('spike', `🔥 ${p.sym} Volume Spike! Price: ${fmt(p.price)}`);
      evaluateTrade(p);
    }

    // Check existing trade
    if (state.activeTrades[p.sym]) {
      const trade = state.activeTrades[p.sym];
      const pChange = (p.price - trade.entry) / trade.entry;

      if (pChange >= state.settings.takeProfit / 100) {
        closeTrade(p, 'profit', pChange);
      } else if (pChange <= -(state.settings.stopLoss / 100)) {
        closeTrade(p, 'loss', pChange);
      }
    }
  });

  updateDashboard();
}

function evaluateTrade(p) {
  if (state.activeTrades[p.sym]) return;
  if (state.dailyLoss >= state.settings.maxLoss) return;

  // Place simulated buy
  state.activeTrades[p.sym] = { entry: p.price, qty: state.settings.tradeSize / p.price };
  addLog('buy', `⚡ BUY ${p.sym} @ ${fmt(p.price)}  ($${state.settings.tradeSize})`);
}

function closeTrade(p, type, pChange) {
  const trade = state.activeTrades[p.sym];
  if (!trade) return;

  const profit = pChange * state.settings.tradeSize;
  state.totalProfit += profit;
  state.tradeCount++;

  if (type === 'profit') {
    state.winCount++;
    addLog('profit', `💰 SELL-PROFIT ${p.sym} | +$${Math.abs(profit).toFixed(3)} (${(pChange*100).toFixed(2)}%)`);
    if (state.settings.notifications) showToast(`Alhamdulillah! An samu $${Math.abs(profit).toFixed(3)} riba a ${p.sym} 🎉`);
  } else {
    state.dailyLoss += Math.abs(profit);
    addLog('loss', `🛡️ SELL-LOSS ${p.sym} | -$${Math.abs(profit).toFixed(3)} (${(pChange*100).toFixed(2)}%)`);
    if (state.dailyLoss >= state.settings.maxLoss) {
      showToast('⚠️ Daily drawdown limit ($' + state.settings.maxLoss + ') reached. Trading paused.', true);
    }
  }

  delete state.activeTrades[p.sym];

  // Push to chart
  profitData.push(state.totalProfit);
  if (profitData.length > MAX_POINTS) profitData.shift();
}

// ── Log ───────────────────────────────────────────────────
function addLog(type, message) {
  const list = document.getElementById('log-list');
  const empty = list.querySelector('.log-empty');
  if (empty) empty.remove();

  const now = new Date();
  const time = now.toTimeString().slice(0, 8);

  const el = document.createElement('div');
  el.className = `log-item ${type}`;
  el.innerHTML = `<span class="log-time">${time}</span> ${message}`;
  list.prepend(el);

  // Keep max 80 items
  while (list.children.length > 80) list.removeChild(list.lastChild);
}

function clearLog() {
  const list = document.getElementById('log-list');
  list.innerHTML = '<div class="log-empty">Log cleared.</div>';
}

// ── Dashboard Update ───────────────────────────────────────
function updateDashboard() {
  // Profit
  const pEl = document.getElementById('total-profit');
  pEl.textContent = (state.totalProfit >= 0 ? '+' : '') + '$' + Math.abs(state.totalProfit).toFixed(3);
  pEl.style.color = state.totalProfit >= 0 ? 'var(--green)' : 'var(--red)';

  const startBal = 100;
  const pct = (state.totalProfit / startBal * 100).toFixed(2);
  document.getElementById('profit-percent').textContent = (pct >= 0 ? '+' : '') + pct + '% of capital';

  // Trades
  document.getElementById('trade-count').textContent = state.tradeCount;
  const wr = state.tradeCount ? ((state.winCount / state.tradeCount) * 100).toFixed(0) : 0;
  document.getElementById('win-rate').textContent = `Win Rate: ${wr}%`;

  // Daily Loss
  document.getElementById('daily-loss').textContent = '$' + state.dailyLoss.toFixed(3);
  const lossRatio = Math.min((state.dailyLoss / state.settings.maxLoss) * 100, 100);
  document.getElementById('risk-bar').style.width = lossRatio + '%';

  // Active
  document.getElementById('active-count').textContent = Object.keys(state.activeTrades).length;

  // Chart
  drawChart();
}

// ── Controls ──────────────────────────────────────────────
function toggleEngine() {
  state.running = !state.running;
  const btn = document.getElementById('toggle-btn');
  const badge = document.getElementById('engine-status');

  if (state.running) {
    btn.textContent = '⏹ Stop Engine';
    btn.classList.remove('start');
    badge.className = 'status-badge status-active';
    badge.innerHTML = '<span class="pulse-dot"></span> ENGINE ACTIVE';
    showToast('Engine restarted. Bot is watching the market. ⚡');
  } else {
    btn.textContent = '▶ Start Engine';
    btn.classList.add('start');
    badge.className = 'status-badge status-stopped';
    badge.innerHTML = '⏹ ENGINE STOPPED';
    showToast('Engine stopped. All open positions are safe. 🛑', true);
  }
}

function saveSettings() {
  state.settings.tradeSize   = parseFloat(document.getElementById('trade-size').value) || 10;
  state.settings.takeProfit  = parseFloat(document.getElementById('take-profit').value) || 0.8;
  state.settings.stopLoss    = parseFloat(document.getElementById('stop-loss').value) || 1.5;
  state.settings.maxLoss     = parseFloat(document.getElementById('max-loss').value) || 5;
  state.settings.spikeThresh = parseFloat(document.getElementById('spike-thresh').value) || 1.5;
  state.settings.notifications = document.getElementById('notif-toggle').checked;

  showToast('✅ Settings saved successfully!');
  addLog('spike', `⚙️ Settings updated: Trade $${state.settings.tradeSize} | TP ${state.settings.takeProfit}% | SL ${state.settings.stopLoss}%`);
}

// ── Toast ─────────────────────────────────────────────────
let toastTimer;
function showToast(msg, isErr = false) {
  const t = document.getElementById('toast');
  t.textContent = msg;
  t.className = 'toast' + (isErr ? ' error' : '');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => { t.className = 'toast hidden'; }, 4000);
}

// ── Boot ──────────────────────────────────────────────────
buildRadar();
updateDashboard();
addLog('spike', '🚀 Aurora Vortex Scalper activated. Monitoring 10 pairs...');

// Simulate price ticks every 800ms
setInterval(simulateTick, 800);

// Redraw chart on resize
window.addEventListener('resize', drawChart);
