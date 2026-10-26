/**
 * 🎰 Slot Machine Game — WebGL / HTML5 Playable Engine
 * Matches Unity C# Scripts: GameManager, SlotMachineController, ReelController, RNGService
 */

(function () {
  'use strict';

  // -------------------------------------------------------------------------
  // Symbol Definitions & Paytable
  // -------------------------------------------------------------------------
  const SYMBOLS = {
    seven: { id: 'seven', name: 'Lucky 7', sprite: 'assets/slot-symbol1.png', payout: 10, weight: 10, isWild: false },
    bar:   { id: 'bar',   name: 'Bar',     sprite: 'assets/slot-symbol4.png', payout: 5,  weight: 20, isWild: false },
    bell:  { id: 'bell',  name: 'Bell',    sprite: 'assets/slot-symbol3.png', payout: 3,  weight: 30, isWild: false },
    cherry:{ id: 'cherry',name: 'Cherry',  sprite: 'assets/slot-symbol2.png', payout: 2,  weight: 35, isWild: false },
    wild:  { id: 'wild',  name: 'Wild',    sprite: null,                      payout: 0,  weight: 5,  isWild: true  }
  };

  // Virtual Reel Tape (20 symbols — exactly matches ReelStrip.cs & SceneSetup.md)
  const STRIP = [
    'cherry', 'bell', 'cherry', 'bar', 'cherry',
    'bell', 'seven', 'cherry', 'wild', 'bar',
    'bell', 'cherry', 'bar', 'bell', 'cherry',
    'seven', 'bell', 'bar', 'cherry', 'bell'
  ];

  // -------------------------------------------------------------------------
  // Audio Synthesizer (Web Audio API — Zero External Dependencies)
  // -------------------------------------------------------------------------
  class SoundManager {
    constructor() {
      this.ctx = null;
      this.enabled = true;
      this.spinOsc = null;
    }

    init() {
      if (!this.ctx) {
        const AudioCtx = window.AudioContext || window.webkitAudioContext;
        this.ctx = new AudioCtx();
      }
      if (this.ctx && this.ctx.state === 'suspended') {
        this.ctx.resume();
      }
    }

    playClick() {
      if (!this.enabled) return;
      this.init();
      const osc = this.ctx.createOscillator();
      const gain = this.ctx.createGain();
      osc.type = 'triangle';
      osc.frequency.setValueAtTime(600, this.ctx.currentTime);
      osc.frequency.exponentialRampToValueAtTime(120, this.ctx.currentTime + 0.05);
      gain.gain.setValueAtTime(0.3, this.ctx.currentTime);
      gain.gain.linearRampToValueAtTime(0.01, this.ctx.currentTime + 0.05);
      osc.connect(gain);
      gain.connect(this.ctx.destination);
      osc.start();
      osc.stop(this.ctx.currentTime + 0.05);
    }

    playLever() {
      if (!this.enabled) return;
      this.init();
      // Mechanical clonk + spring
      const osc = this.ctx.createOscillator();
      const gain = this.ctx.createGain();
      osc.type = 'sawtooth';
      osc.frequency.setValueAtTime(220, this.ctx.currentTime);
      osc.frequency.exponentialRampToValueAtTime(80, this.ctx.currentTime + 0.15);
      gain.gain.setValueAtTime(0.4, this.ctx.currentTime);
      gain.gain.linearRampToValueAtTime(0.01, this.ctx.currentTime + 0.15);
      osc.connect(gain);
      gain.connect(this.ctx.destination);
      osc.start();
      osc.stop(this.ctx.currentTime + 0.15);
    }

    startSpinLoop() {
      if (!this.enabled) return;
      this.init();
      if (this.spinOsc) return;

      const osc = this.ctx.createOscillator();
      const gain = this.ctx.createGain();
      osc.type = 'triangle';
      osc.frequency.setValueAtTime(140, this.ctx.currentTime);
      gain.gain.setValueAtTime(0.08, this.ctx.currentTime);
      osc.connect(gain);
      gain.connect(this.ctx.destination);
      osc.start();
      this.spinOsc = { osc, gain };
    }

    stopSpinLoop() {
      if (this.spinOsc) {
        try {
          this.spinOsc.gain.gain.linearRampToValueAtTime(0.001, this.ctx.currentTime + 0.05);
          this.spinOsc.osc.stop(this.ctx.currentTime + 0.06);
        } catch (_) {}
        this.spinOsc = null;
      }
    }

    playReelStop() {
      if (!this.enabled) return;
      this.init();
      // Sharp mechanical snap
      const osc = this.ctx.createOscillator();
      const gain = this.ctx.createGain();
      osc.type = 'sine';
      osc.frequency.setValueAtTime(320, this.ctx.currentTime);
      osc.frequency.exponentialRampToValueAtTime(80, this.ctx.currentTime + 0.08);
      gain.gain.setValueAtTime(0.5, this.ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.01, this.ctx.currentTime + 0.08);
      osc.connect(gain);
      gain.connect(this.ctx.destination);
      osc.start();
      osc.stop(this.ctx.currentTime + 0.08);
    }

    playWin(isJackpot) {
      if (!this.enabled) return;
      this.init();
      const notes = isJackpot ? [523.25, 659.25, 783.99, 1046.50, 1318.51] : [440, 554.37, 659.25];
      notes.forEach((freq, idx) => {
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();
        const start = this.ctx.currentTime + idx * 0.1;
        osc.type = 'triangle';
        osc.frequency.setValueAtTime(freq, start);
        gain.gain.setValueAtTime(0.4, start);
        gain.gain.exponentialRampToValueAtTime(0.01, start + 0.35);
        osc.connect(gain);
        gain.connect(this.ctx.destination);
        osc.start(start);
        osc.stop(start + 0.35);
      });
    }

    playBonus() {
      if (!this.enabled) return;
      this.init();
      [600, 800, 1000, 1200].forEach((freq, idx) => {
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();
        const start = this.ctx.currentTime + idx * 0.08;
        osc.type = 'sine';
        osc.frequency.setValueAtTime(freq, start);
        gain.gain.setValueAtTime(0.3, start);
        gain.gain.exponentialRampToValueAtTime(0.01, start + 0.2);
        osc.connect(gain);
        gain.connect(this.ctx.destination);
        osc.start(start);
        osc.stop(start + 0.2);
      });
    }
  }

  // -------------------------------------------------------------------------
  // Particle Confetti Engine for Jackpot
  // -------------------------------------------------------------------------
  class ConfettiEngine {
    constructor(canvas) {
      this.canvas = canvas;
      this.ctx = canvas.getContext('2d');
      this.particles = [];
      this.active = false;
      this.resize();
      window.addEventListener('resize', () => this.resize());
    }

    resize() {
      this.canvas.width = window.innerWidth;
      this.canvas.height = window.innerHeight;
    }

    burst() {
      this.particles = [];
      const colors = ['#ffd700', '#ff2a5f', '#00f0ff', '#39ff14', '#ffffff', '#ffaa00'];
      for (let i = 0; i < 160; i++) {
        this.particles.push({
          x: this.canvas.width * (0.35 + Math.random() * 0.3),
          y: this.canvas.height * 0.45,
          vx: (Math.random() - 0.5) * 18,
          vy: -Math.random() * 16 - 6,
          size: Math.random() * 8 + 4,
          color: colors[Math.floor(Math.random() * colors.length)],
          rotation: Math.random() * 360,
          rotSpeed: (Math.random() - 0.5) * 15,
          gravity: 0.35,
          life: 1,
          decay: Math.random() * 0.015 + 0.01
        });
      }
      this.active = true;
      this.animate();
    }

    animate() {
      if (!this.active) return;
      this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

      let aliveCount = 0;
      for (let p of this.particles) {
        if (p.life > 0) {
          p.x += p.vx;
          p.vy += p.gravity;
          p.y += p.vy;
          p.rotation += p.rotSpeed;
          p.life -= p.decay;

          this.ctx.save();
          this.ctx.translate(p.x, p.y);
          this.ctx.rotate((p.rotation * Math.PI) / 180);
          this.ctx.fillStyle = p.color;
          this.ctx.globalAlpha = Math.max(0, p.life);
          this.ctx.fillRect(-p.size / 2, -p.size / 2, p.size, p.size * 1.4);
          this.ctx.restore();
          aliveCount++;
        }
      }

      if (aliveCount > 0) {
        requestAnimationFrame(() => this.animate());
      } else {
        this.active = false;
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
      }
    }
  }

  // -------------------------------------------------------------------------
  // Main Game Application
  // -------------------------------------------------------------------------
  class SlotGameApp {
    constructor() {
      this.audio = new SoundManager();
      this.confetti = new ConfettiEngine(document.getElementById('confettiCanvas'));

      // State
      this.credits = 1000;
      this.bet = 10;
      this.lastWin = 0;
      this.streak = 0;
      this.freeSpins = 0;
      this.isSpinning = false;
      this.autoSpinActive = false;
      this.autoSpinsRemaining = 0;

      // Reel state
      this.reels = [
        { id: 1, currentIndex: 0, targetIndex: 0, isRolling: false },
        { id: 2, currentIndex: 4, targetIndex: 0, isRolling: false },
        { id: 3, currentIndex: 8, targetIndex: 0, isRolling: false }
      ];

      this.cacheDOM();
      this.bindEvents();
      this.renderReels();
      this.updateHUD();
    }

    cacheDOM() {
      this.creditsDisplay = document.getElementById('creditsDisplay');
      this.betDisplay = document.getElementById('betDisplay');
      this.lastWinDisplay = document.getElementById('lastWinDisplay');
      this.streakBadge = document.getElementById('streakBadge');
      this.streakCount = document.getElementById('streakCount');
      this.freeSpinBadge = document.getElementById('freeSpinBadge');
      this.freeSpinCount = document.getElementById('freeSpinCount');

      this.spinBtn = document.getElementById('spinBtn');
      this.autoSpinBtn = document.getElementById('autoSpinBtn');
      this.autoBtnText = document.getElementById('autoBtnText');
      this.betMinusBtn = document.getElementById('betMinusBtn');
      this.betPlusBtn = document.getElementById('betPlusBtn');
      this.presetPills = document.querySelectorAll('.preset-pill');

      this.leverContainer = document.getElementById('leverContainer');
      this.leverHotspot = document.getElementById('leverHotspot');
      this.winBanner = document.getElementById('winBanner');
      this.winHeadline = document.getElementById('winHeadline');
      this.winDetail = document.getElementById('winDetail');
      this.screenFlash = document.getElementById('screenFlash');

      this.paytablePanel = document.getElementById('paytablePanel');
      this.paytableBtn = document.getElementById('paytableBtn');
      this.closePaytableBtn = document.getElementById('closePaytableBtn');

      this.modalOverlay = document.getElementById('modalOverlay');
      this.popupHeadline = document.getElementById('popupHeadline');
      this.popupBody = document.getElementById('popupBody');
      this.popupYesBtn = document.getElementById('popupYesBtn');
      this.popupNoBtn = document.getElementById('popupNoBtn');

      this.audioToggleBtn = document.getElementById('audioToggleBtn');
      this.soundIcon = document.getElementById('soundIcon');
    }

    bindEvents() {
      // User interaction init for AudioContext
      const unlockAudio = () => this.audio.init();
      window.addEventListener('click', unlockAudio, { once: true });
      window.addEventListener('keydown', unlockAudio, { once: true });

      // Spin triggers
      this.spinBtn.addEventListener('click', () => this.handleSpin());
      this.leverHotspot.addEventListener('click', () => this.pullLever());

      // Keyboard spacebar to spin
      window.addEventListener('keydown', (e) => {
        if (e.code === 'Space' && !this.isSpinning && this.modalOverlay.classList.contains('hidden')) {
          e.preventDefault();
          this.handleSpin();
        }
      });

      // Bet controls
      this.betMinusBtn.addEventListener('click', () => {
        this.setBet(this.bet - 10);
        this.audio.playClick();
      });
      this.betPlusBtn.addEventListener('click', () => {
        this.setBet(this.bet + 10);
        this.audio.playClick();
      });

      this.presetPills.forEach((pill) => {
        pill.addEventListener('click', () => {
          const val = parseInt(pill.dataset.bet, 10);
          this.setBet(val);
          this.audio.playClick();
        });
      });

      // Auto-Spin toggle
      this.autoSpinBtn.addEventListener('click', () => this.toggleAutoSpin());

      // Paytable
      this.paytableBtn.addEventListener('click', () => {
        this.paytablePanel.classList.remove('hidden');
        this.audio.playClick();
      });
      this.closePaytableBtn.addEventListener('click', () => {
        this.paytablePanel.classList.add('hidden');
        this.audio.playClick();
      });

      // Sound toggle
      this.audioToggleBtn.addEventListener('click', () => {
        this.audio.enabled = !this.audio.enabled;
        this.soundIcon.textContent = this.audio.enabled ? '🔊' : '🔇';
      });

      // Popup buttons
      this.popupYesBtn.addEventListener('click', () => {
        this.hidePopup();
        this.audio.playClick();
      });
      this.popupNoBtn.addEventListener('click', () => {
        this.hidePopup();
        this.audio.playClick();
        setTimeout(() => this.handleSpin(), 300);
      });
    }

    setBet(amount) {
      if (this.isSpinning) return;
      this.bet = Math.max(10, Math.min(amount, 500));
      this.presetPills.forEach((p) => {
        p.classList.toggle('active', parseInt(p.dataset.bet, 10) === this.bet);
      });
      this.updateHUD();
    }

    updateHUD() {
      this.creditsDisplay.textContent = this.credits.toLocaleString();
      this.betDisplay.textContent = this.bet.toLocaleString();
      this.lastWinDisplay.textContent = this.lastWin.toLocaleString();

      // Streak badge
      if (this.streak >= 2) {
        this.streakBadge.classList.remove('hidden');
        this.streakCount.textContent = this.streak;
      } else {
        this.streakBadge.classList.add('hidden');
      }

      // Free spin badge
      if (this.freeSpins > 0) {
        this.freeSpinBadge.classList.remove('hidden');
        this.freeSpinCount.textContent = this.freeSpins;
      } else {
        this.freeSpinBadge.classList.add('hidden');
      }

      // Disable/enable controls
      this.spinBtn.disabled = this.isSpinning;
      this.betMinusBtn.disabled = this.isSpinning || this.bet <= 10;
      this.betPlusBtn.disabled = this.isSpinning || this.bet >= 500;
    }

    pullLever() {
      if (this.isSpinning) return;
      this.leverContainer.classList.add('lever-pulled');
      this.audio.playLever();

      setTimeout(() => {
        this.leverContainer.classList.remove('lever-pulled');
        this.handleSpin();
      }, 180);
    }

    toggleAutoSpin() {
      if (this.autoSpinActive) {
        this.autoSpinActive = false;
        this.autoSpinBtn.classList.remove('active');
        this.autoBtnText.textContent = 'AUTO';
      } else {
        this.autoSpinActive = true;
        this.autoSpinsRemaining = 10;
        this.autoSpinBtn.classList.add('active');
        this.autoBtnText.textContent = 'STOP (10)';
        if (!this.isSpinning) {
          this.handleSpin();
        }
      }
    }

    // -----------------------------------------------------------------------
    // Weighted RNG (Fair Accumulated Weight Algorithm)
    // -----------------------------------------------------------------------
    getRandomSymbol() {
      const pool = Object.values(SYMBOLS);
      const totalWeight = pool.reduce((acc, s) => acc + s.weight, 0);
      let roll = Math.floor(Math.random() * totalWeight);

      for (let sym of pool) {
        if (roll < sym.weight) return sym;
        roll -= sym.weight;
      }
      return pool[0];
    }

    findSymbolIndexOnStrip(symbolId) {
      const indices = [];
      STRIP.forEach((id, idx) => {
        if (id === symbolId) indices.push(idx);
      });
      return indices.length > 0 ? indices[Math.floor(Math.random() * indices.length)] : 0;
    }

    // -----------------------------------------------------------------------
    // Reel Render & Animation
    // -----------------------------------------------------------------------
    renderReels() {
      this.reels.forEach((r, idx) => {
        this.updateReelSlots(idx + 1, r.currentIndex);
      });
    }

    updateReelSlots(reelNum, midIndex) {
      const len = STRIP.length;
      const topIdx = (midIndex - 1 + len) % len;
      const botIdx = (midIndex + 1) % len;

      this.renderSlot(`r${reelNum}_top`, STRIP[topIdx]);
      this.renderSlot(`r${reelNum}_mid`, STRIP[midIndex]);
      this.renderSlot(`r${reelNum}_bot`, STRIP[botIdx]);
    }

    renderSlot(elementId, symbolId) {
      const el = document.getElementById(elementId);
      if (!el) return;
      el.innerHTML = '';

      const sym = SYMBOLS[symbolId];
      if (sym.isWild) {
        const star = document.createElement('div');
        star.className = 'wild-symbol';
        star.textContent = '⭐';
        el.appendChild(star);
      } else {
        const img = document.createElement('img');
        img.src = sym.sprite;
        img.alt = sym.name;
        img.draggable = false;
        el.appendChild(img);
      }
    }

    // -----------------------------------------------------------------------
    // Spin Sequence Orchestrator
    // -----------------------------------------------------------------------
    handleSpin() {
      if (this.isSpinning) return;

      // Handle Free Spin vs Normal Bet
      if (this.freeSpins > 0) {
        this.freeSpins--;
      } else {
        if (this.credits < this.bet) {
          this.showWinBanner('⚠ NO CREDITS', 'Lower your bet to continue!');
          this.autoSpinActive = false;
          this.autoSpinBtn.classList.remove('active');
          this.autoBtnText.textContent = 'AUTO';
          return;
        }
        this.credits -= this.bet;
      }

      this.isSpinning = true;
      this.winBanner.classList.add('hidden');
      this.clearReelGlows();
      this.updateHUD();
      this.audio.startSpinLoop();

      // Pre-determine outcomes upfront via weighted RNG (Fair RNG)
      const targetSymbols = [
        this.getRandomSymbol(),
        this.getRandomSymbol(),
        this.getRandomSymbol()
      ];

      this.reels.forEach((r, idx) => {
        r.targetIndex = this.findSymbolIndexOnStrip(targetSymbols[idx].id);
      });

      // Animate reels with staggered stop times
      const durations = [1600, 2050, 2500]; // 450ms stagger
      let completed = 0;

      this.reels.forEach((r, idx) => {
        this.spinReel(idx, durations[idx], () => {
          completed++;
          this.audio.playReelStop();
          if (completed === this.reels.length) {
            this.audio.stopSpinLoop();
            this.evaluateSpin(targetSymbols);
          }
        });
      });
    }

    spinReel(reelIndex, duration, onComplete) {
      const reel = this.reels[reelIndex];
      const reelNum = reelIndex + 1;
      const startTime = performance.now();
      const speed = 18; // symbols per second during fast phase
      let currentStripPos = reel.currentIndex;

      const animate = (now) => {
        const elapsed = now - startTime;
        if (elapsed < duration - 500) {
          // Fast spin phase
          currentStripPos = (currentStripPos + 0.5) % STRIP.length;
          this.updateReelSlots(reelNum, Math.floor(currentStripPos));
          requestAnimationFrame(animate);
        } else if (elapsed < duration) {
          // Decelerate & snap phase
          const progress = (elapsed - (duration - 500)) / 500;
          const eased = Math.sin((progress * Math.PI) / 2);
          if (progress > 0.8) {
            reel.currentIndex = reel.targetIndex;
            this.updateReelSlots(reelNum, reel.currentIndex);
          } else {
            currentStripPos = (currentStripPos + 0.25 * (1 - eased)) % STRIP.length;
            this.updateReelSlots(reelNum, Math.floor(currentStripPos));
          }
          requestAnimationFrame(animate);
        } else {
          // Complete snap
          reel.currentIndex = reel.targetIndex;
          this.updateReelSlots(reelNum, reel.currentIndex);
          onComplete();
        }
      };

      requestAnimationFrame(animate);
    }

    // -----------------------------------------------------------------------
    // Win Evaluation
    // -----------------------------------------------------------------------
    evaluateSpin(symbols) {
      const [s1, s2, s3] = symbols;
      let winType = 'none';
      let multiplier = 0;
      let winningSymbol = null;

      const wilds = symbols.filter(s => s.isWild);
      const nonWilds = symbols.filter(s => !s.isWild);

      // 1. Check Full 3-Match (with wild substitution)
      if (wilds.length === 3 || (nonWilds.length > 0 && nonWilds.every(s => s.id === nonWilds[0].id))) {
        winningSymbol = nonWilds.length > 0 ? nonWilds[0] : SYMBOLS.seven;
        if (winningSymbol.id === 'seven') {
          winType = 'jackpot';
          multiplier = 10;
        } else {
          winType = 'bigwin';
          multiplier = winningSymbol.payout;
        }
      }
      // 2. Wild Bonus (≥ 2 Wilds without full match)
      else if (wilds.length >= 2) {
        winType = 'wildbonus';
      }
      // 3. Any 2 matching symbols
      else if (
        (s1.id === s2.id && !s1.isWild) ||
        (s2.id === s3.id && !s2.isWild) ||
        (s1.id === s3.id && !s1.isWild)
      ) {
        winType = 'smallwin';
        multiplier = 1;
      }

      // Process Payout
      let payout = 0;
      if (winType === 'jackpot') {
        payout = this.bet * multiplier;
        this.credits += payout;
        this.lastWin = payout;
        this.streak++;
        this.highlightReels();
        this.triggerJackpotFX();
        this.showWinBanner('🎰 JACKPOT! 🎰', `+${payout} CREDITS!`);
        this.audio.playWin(true);
        setTimeout(() => {
          this.showPopup('🎰 JACKPOT WINNER!', `You hit 3 Lucky 7s and won ${payout} Credits!`);
        }, 700);
      } else if (winType === 'bigwin') {
        payout = this.bet * multiplier;
        this.credits += payout;
        this.lastWin = payout;
        this.streak++;
        this.highlightReels();
        this.showWinBanner('⭐ BIG WIN! ⭐', `3x ${winningSymbol.name} (+${payout})`);
        this.audio.playWin(false);
        setTimeout(() => {
          this.showPopup('⭐ BIG WIN!', `Matched 3x ${winningSymbol.name}!\n+${payout} Credits awarded.`);
        }, 600);
      } else if (winType === 'wildbonus') {
        this.freeSpins += 2;
        this.streak++;
        this.highlightReels();
        this.showWinBanner('🌟 WILD BONUS! 🌟', '+2 FREE SPINS AWARDED!');
        this.audio.playBonus();
      } else if (winType === 'smallwin') {
        payout = this.bet * multiplier;
        this.credits += payout;
        this.lastWin = payout;
        this.streak++;
        this.showWinBanner('PAIR MATCH', `+${payout} Bet Returned`);
        this.audio.playWin(false);
      } else {
        this.streak = 0;
        this.lastWin = 0;
      }

      this.isSpinning = false;
      this.updateHUD();

      // Auto-Spin progression
      if (this.autoSpinActive) {
        this.autoSpinsRemaining--;
        if (this.autoSpinsRemaining > 0 && this.credits >= this.bet) {
          this.autoBtnText.textContent = `STOP (${this.autoSpinsRemaining})`;
          setTimeout(() => this.handleSpin(), 1500);
        } else {
          this.autoSpinActive = false;
          this.autoSpinBtn.classList.remove('active');
          this.autoBtnText.textContent = 'AUTO';
        }
      }
    }

    highlightReels() {
      for (let i = 1; i <= 3; i++) {
        document.getElementById(`r${i}_glow`)?.classList.add('active');
      }
    }

    clearReelGlows() {
      for (let i = 1; i <= 3; i++) {
        document.getElementById(`r${i}_glow`)?.classList.remove('active');
      }
    }

    triggerJackpotFX() {
      this.screenFlash.classList.add('active');
      setTimeout(() => this.screenFlash.classList.remove('active'), 350);
      this.confetti.burst();
    }

    showWinBanner(headline, detail) {
      this.winHeadline.textContent = headline;
      this.winDetail.textContent = detail;
      this.winBanner.classList.remove('hidden');
    }

    showPopup(title, msg) {
      this.popupHeadline.textContent = title;
      this.popupBody.textContent = msg;
      this.modalOverlay.classList.remove('hidden');
    }

    hidePopup() {
      this.modalOverlay.classList.add('hidden');
    }
  }

  // Initialize once DOM is ready
  window.addEventListener('DOMContentLoaded', () => {
    window.slotApp = new SlotGameApp();
  });
})();
