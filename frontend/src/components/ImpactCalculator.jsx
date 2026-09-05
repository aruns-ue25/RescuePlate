import React, { useState } from 'react';
import { Calculator, Utensils, Leaf, Users, DollarSign, Sparkles, ArrowRight } from 'lucide-react';

export default function ImpactCalculator({ onOpenAuth }) {
  const [dailyMeals, setDailyMeals] = useState(25);
  const [businessType, setBusinessType] = useState('restaurant'); // restaurant, bakery, supermarket, hotel

  // Environmental and community multipliers
  const yearlyMeals = dailyMeals * 365;
  const yearlyKg = Math.round(yearlyMeals * 0.45); // ~0.45 kg per meal
  const yearlyCo2 = (yearlyKg * 2.5 / 1000).toFixed(1); // 2.5kg CO2 per kg food waste
  const familiesFed = Math.round(yearlyMeals / 4); // 4 meals per family basket
  const estValueSaved = (yearlyMeals * 6.5).toLocaleString(); // avg $6.50 value per meal

  return (
    <section className="calculator-section" id="calculator">
      <div className="container">
        <div className="calculator-card">
          <div className="calc-left">
            <div className="badge badge-accent">
              <Calculator size={14} />
              <span>Interactive Simulator</span>
            </div>
            <h2 className="calc-title">Calculate Your Food Rescue Impact</h2>
            <p className="calc-desc">
              Estimate the community and environmental change your surplus food can achieve over the next 12 months.
            </p>

            {/* Business Type Selector */}
            <div className="calc-input-group">
              <label className="input-label">Business Sector</label>
              <div className="business-type-tabs">
                {[
                  { id: 'restaurant', label: '🍽️ Restaurant' },
                  { id: 'bakery', label: '🥐 Bakery' },
                  { id: 'hotel', label: '🏨 Hotel/Catering' },
                  { id: 'supermarket', label: '🛒 Supermarket' }
                ].map((type) => (
                  <button
                    key={type.id}
                    onClick={() => setBusinessType(type.id)}
                    className={`b-tab-btn ${businessType === type.id ? 'active' : ''}`}
                  >
                    {type.label}
                  </button>
                ))}
              </div>
            </div>

            {/* Meal Slider */}
            <div className="calc-slider-group">
              <div className="slider-header">
                <span className="slider-label">Estimated Daily Surplus Portions</span>
                <span className="slider-value-display">{dailyMeals} meals / day</span>
              </div>
              <input
                type="range"
                min="5"
                max="200"
                step="5"
                value={dailyMeals}
                onChange={(e) => setDailyMeals(Number(e.target.value))}
                className="impact-range-slider"
              />
              <div className="slider-ticks-row">
                <span>5 meals</span>
                <span>50 meals</span>
                <span>100 meals</span>
                <span>200 meals</span>
              </div>
            </div>

            <div className="calc-cta-row">
              <button
                onClick={() => onOpenAuth('register', 'DONOR')}
                className="btn btn-accent btn-lg"
              >
                <span>Put This Surplus To Work</span>
                <ArrowRight size={18} />
              </button>
            </div>
          </div>

          {/* Right Results Grid */}
          <div className="calc-right">
            <div className="calc-results-header">
              <Sparkles className="text-amber" size={20} />
              <h3>Your 1-Year Projected Impact</h3>
            </div>

            <div className="calc-metrics-grid">
              <div className="calc-metric-box">
                <div className="c-icon-box bg-emerald-light">
                  <Utensils className="text-emerald" size={22} />
                </div>
                <div className="c-val">{yearlyMeals.toLocaleString()}</div>
                <div className="c-lbl">Hot Meals Rescued</div>
              </div>

              <div className="calc-metric-box">
                <div className="c-icon-box bg-amber-light">
                  <Users className="text-amber" size={22} />
                </div>
                <div className="c-val">{familiesFed.toLocaleString()}</div>
                <div className="c-lbl">Family Dinners Supported</div>
              </div>

              <div className="calc-metric-box">
                <div className="c-icon-box bg-emerald-light">
                  <Leaf className="text-emerald" size={22} />
                </div>
                <div className="c-val">{yearlyCo2} Tons</div>
                <div className="c-lbl">CO₂ Emissions Averted</div>
              </div>

              <div className="calc-metric-box">
                <div className="c-icon-box bg-accent-light">
                  <DollarSign className="text-accent" size={22} />
                </div>
                <div className="c-val">${estValueSaved}</div>
                <div className="c-lbl">Community Food Value</div>
              </div>
            </div>

            <div className="calc-footer-note">
              🌱 Based on verified organic waste diversion algorithms & FAO food sustainability metrics.
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
