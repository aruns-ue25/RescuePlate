import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { 
  PlusCircle, 
  Search, 
  CheckCircle, 
  Truck, 
  HeartHandshake, 
  BarChart3, 
  Sparkles,
  ArrowRight,
  ShieldCheck,
  Package
} from 'lucide-react';

export default function HowItWorks() {
  const [activeRole, setActiveRole] = useState('donor'); // 'donor' or 'org'

  const donorSteps = [
    {
      step: "01",
      icon: <PlusCircle size={26} className="text-emerald" />,
      title: "List Surplus Food in Seconds",
      description: "Specify food type, quantity, packaging, dietary tags, pickup window, and collection method (Pickup or Delivery)."
    },
    {
      step: "02",
      icon: <CheckCircle size={26} className="text-emerald" />,
      title: "Review & Accept Requests",
      description: "Receive instant notifications when charities request food. Accept full or partial quantities with automated stock management."
    },
    {
      step: "03",
      icon: <Truck size={26} className="text-emerald" />,
      title: "Handover or Dispatch",
      description: "The charity collects the food during the designated window, or your team delivers it directly to their community pantry."
    },
    {
      step: "04",
      icon: <BarChart3 size={26} className="text-emerald" />,
      title: "Track ESG & Food Rescue Impact",
      description: "Upon receipt confirmation, your profile updates with meals donated, carbon offset numbers, and corporate social responsibility metrics."
    }
  ];

  const orgSteps = [
    {
      step: "01",
      icon: <Search size={26} className="text-amber" />,
      title: "Discover Fresh Local Surplus",
      description: "Search nearby restaurants, bakeries, and grocers with real-time countdown timers on active donations."
    },
    {
      step: "02",
      icon: <Package size={26} className="text-amber" />,
      title: "Request Custom Quantities",
      description: "Request the exact number of portions or kilograms your shelter needs. Partial fulfilment allows multiple charities to benefit."
    },
    {
      step: "03",
      icon: <HeartHandshake size={26} className="text-amber" />,
      title: "Collect or Receive Delivery",
      description: "Coordinate with the donor for a seamless handover following standard safe food transit guidelines."
    },
    {
      step: "04",
      icon: <ShieldCheck size={26} className="text-amber" />,
      title: "Confirm Receipt with 1-Click",
      description: "Confirm food arrival to officially complete the donation lifecycle and update community impact statistics."
    }
  ];

  const currentSteps = activeRole === 'donor' ? donorSteps : orgSteps;

  return (
    <section className="how-it-works-section" id="how-it-works">
      <div className="container">
        <div className="section-header text-center">
          <div className="badge badge-primary">
            <Sparkles size={14} />
            <span>Seamless 4-Step Process</span>
          </div>
          <h2 className="section-title">How RescuePlate Connects Us</h2>
          <p className="section-subtitle">
            A frictionless, transparent, and dignified redistribution network designed for zero food waste.
          </p>

          {/* Interactive Role Switcher Toggle */}
          <div className="workflow-toggle-container">
            <button
              onClick={() => setActiveRole('donor')}
              className={`workflow-toggle-btn ${activeRole === 'donor' ? 'active donor-active' : ''}`}
            >
              <span>For Food Donors (Hotels, Bakeries, Restaurants)</span>
            </button>
            <button
              onClick={() => setActiveRole('org')}
              className={`workflow-toggle-btn ${activeRole === 'org' ? 'active org-active' : ''}`}
            >
              <span>For Charities & Nonprofits (Shelters, Pantries)</span>
            </button>
          </div>
        </div>

        {/* Steps Grid */}
        <div className="steps-grid">
          {currentSteps.map((item, index) => (
            <div key={item.step} className="step-card">
              <div className="step-badge-number">{item.step}</div>
              <div className="step-icon-box">
                {item.icon}
              </div>
              <h3 className="step-card-title">{item.title}</h3>
              <p className="step-card-desc">{item.description}</p>
              {index < currentSteps.length - 1 && (
                <div className="step-connector-arrow">
                  <ArrowRight size={18} />
                </div>
              )}
            </div>
          ))}
        </div>

        {/* Workflow Callout Banner */}
        <div className="workflow-callout-box">
          <div className="callout-text">
            <h4>Ready to make an immediate impact in your neighborhood?</h4>
            <p>
              {activeRole === 'donor'
                ? "Join top restaurants and grocers turning end-of-day surplus into smiles."
                : "Register your charity to receive automatic alerts when fresh food is posted nearby."}
            </p>
          </div>
          <Link
            to="/register"
            className={`btn ${activeRole === 'donor' ? 'btn-primary' : 'btn-amber'} btn-lg`}
          >
            <span>{activeRole === 'donor' ? 'Register as Food Donor' : 'Register as Organization'}</span>
            <ArrowRight size={18} />
          </Link>
        </div>
      </div>
    </section>
  );
}
