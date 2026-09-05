import React from 'react';
import { 
  Split, 
  Clock, 
  Truck, 
  ShieldCheck, 
  BellRing, 
  FileCheck2, 
  Layers
} from 'lucide-react';

export default function FeaturesSection() {
  const features = [
    {
      icon: <Split size={26} className="text-emerald" />,
      title: "Smart Partial Fulfilment",
      description: "Large catering or bakery batches can be split among multiple smaller community charities without exceeding total quantity.",
      tag: "Redistribution"
    },
    {
      icon: <Clock size={26} className="text-amber" />,
      title: "Automated Expiry Safeguards",
      description: "Availability windows ensure donations are prioritized before shelf-life closes, preventing expired listings automatically.",
      tag: "Freshness"
    },
    {
      icon: <Truck size={26} className="text-emerald" />,
      title: "Flexible Handover Modes",
      description: "Support for both Charity Organization Pickup and Donor Direct Delivery, with full handover tracking.",
      tag: "Logistics"
    },
    {
      icon: <ShieldCheck size={26} className="text-amber" />,
      title: "1-Click Receipt Confirmation",
      description: "A donation is only marked Completed after the receiving organization confirms receipt of safe, intact food.",
      tag: "Verification"
    },
    {
      icon: <BellRing size={26} className="text-emerald" />,
      title: "Instant In-App Notifications",
      description: "Real-time alerts for donors and charities on new requests, status changes, delivery milestones, and approvals.",
      tag: "Real-time"
    },
    {
      icon: <FileCheck2 size={26} className="text-amber" />,
      title: "Timestamped Status History",
      description: "Complete transparent audit trail from Posted → Claimed → Collection Arranged → Collected → Completed.",
      tag: "Audit Trail"
    }
  ];

  return (
    <section className="features-section" id="features">
      <div className="container">
        <div className="section-header text-center">
          <div className="badge badge-primary">
            <Layers size={14} />
            <span>Built For Reliability & Trust</span>
          </div>
          <h2 className="section-title">Designed for Seamless Food Redistribution</h2>
          <p className="section-subtitle">
            Every feature in RescuePlate is engineered to eliminate food waste while maintaining strict safety, 
            transparency, and speed.
          </p>
        </div>

        <div className="features-grid">
          {features.map((feature, idx) => (
            <div key={idx} className="feature-card">
              <div className="feature-top-row">
                <div className="feature-icon-box">{feature.icon}</div>
                <span className="feature-tag">{feature.tag}</span>
              </div>
              <h3 className="feature-title">{feature.title}</h3>
              <p className="feature-desc">{feature.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
