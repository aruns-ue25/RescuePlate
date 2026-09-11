import React from 'react';
import { Target, Globe, Heart, Shield, Sparkles, Check, Users2 } from 'lucide-react';

export default function AboutSection() {
  const pillars = [
    {
      title: "Eliminating Waste at the Source",
      description: "Empowering food businesses to convert excess inventory into humanitarian aid instead of landfill costs."
    },
    {
      title: "Dignified, Transparent Access",
      description: "Giving local charities immediate visibility into available quality nutrition with equitable partial distribution."
    },
    {
      title: "Audited Environmental Impact",
      description: "Providing automated ESG metrics, carbon diversion telemetry, and compliance reports for every participant."
    }
  ];

  return (
    <section className="about-section" id="about">
      <div className="container">
        <div className="about-grid">
          {/* Left Column: Story & Vision */}
          <div className="about-left">
            <div className="badge badge-primary">
              <Globe size={14} />
              <span>Our Purpose & Mission</span>
            </div>
            <h2 className="about-title">
              Why We Built <span className="text-gradient">RescuePlate</span>
            </h2>
            <p className="about-paragraph">
              Around the world, more than one-third of all food produced is lost or wasted—generating 
              nearly 10% of global greenhouse gas emissions. At the same time, community food banks 
              and shelters struggle daily to secure nutritious meals for families in need.
            </p>
            <p className="about-paragraph">
              <strong>RescuePlate</strong> was born to bridge this divide through intelligent, 
              community-centered software. By removing the logistical friction between food businesses 
              and verified charities, we make redistributing surplus food faster, safer, and 
              infinitely more impactful than throwing it away.
            </p>

            <div className="sdg-badges-row">
              <div className="sdg-pill sdg-2">
                <span className="sdg-num">SDG 2</span>
                <span className="sdg-label">Zero Hunger</span>
              </div>
              <div className="sdg-pill sdg-12">
                <span className="sdg-num">SDG 12</span>
                <span className="sdg-label">Responsible Consumption</span>
              </div>
              <div className="sdg-pill sdg-13">
                <span className="sdg-num">SDG 13</span>
                <span className="sdg-label">Climate Action</span>
              </div>
            </div>

            <div className="pillars-list">
              {pillars.map((p, idx) => (
                <div key={idx} className="pillar-item">
                  <div className="pillar-check">
                    <Check size={16} className="text-emerald" />
                  </div>
                  <div>
                    <h4 className="pillar-title">{p.title}</h4>
                    <p className="pillar-desc">{p.description}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Right Column: Interactive Mission Showcase Box */}
          <div className="about-right">
            <div className="mission-highlight-card">
              <div className="mission-card-bg-glow"></div>
              <div className="mission-header">
                <span className="badge badge-dark">The Global Challenge vs Our Solution</span>
              </div>

              <div className="crisis-stat-box">
                <div className="crisis-val">1.3 Billion Tons</div>
                <div className="crisis-lbl">Food wasted globally each year</div>
              </div>

              <div className="arrow-down-divider">
                <span>RescuePlate Intercepts Waste</span>
              </div>

              <div className="solution-box">
                <div className="sol-item">
                  <span className="sol-icon">⏱️</span>
                  <div>
                    <strong>Under 15 Minutes</strong>
                    <p>Average time from donor posting to charity claim</p>
                  </div>
                </div>
                <div className="sol-item">
                  <span className="sol-icon">🤝</span>
                  <div>
                    <strong>100% Free for Nonprofits</strong>
                    <p>Zero platform fees for certified shelters and charities</p>
                  </div>
                </div>
                <div className="sol-item">
                  <span className="sol-icon">🛡️</span>
                  <div>
                    <strong>Verified Quality Protocol</strong>
                    <p>Strict food categorization & safe handover confirmation</p>
                  </div>
                </div>
              </div>

              <div className="quote-box">
                <p>
                  <em>"No good meal should ever be thrown away while someone in our community is going to bed hungry."</em>
                </p>
                <span className="quote-author">— RescuePlate Founding Principles</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
