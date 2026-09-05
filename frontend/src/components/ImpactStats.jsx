import React from 'react';
import { 
  UtensilsCrossed, 
  Scale, 
  Store, 
  Building, 
  Leaf, 
  Award 
} from 'lucide-react';

export default function ImpactStats() {
  const stats = [
    {
      id: 1,
      icon: <UtensilsCrossed size={28} className="text-emerald" />,
      number: "48,520+",
      label: "Surplus Meals Rescued",
      description: "Directly delivered to community shelters & pantries",
      trend: "+18% this month"
    },
    {
      id: 2,
      icon: <Scale size={28} className="text-amber" />,
      number: "24.3 Tons",
      label: "Food Kept from Landfills",
      description: "Reducing municipal methane & organic waste",
      trend: "Zero waste goal"
    },
    {
      id: 3,
      icon: <Store size={28} className="text-emerald" />,
      number: "185+",
      label: "Active Food Donors",
      description: "Leading hotels, restaurants, bakeries & grocers",
      trend: "Across 12 districts"
    },
    {
      id: 4,
      icon: <Building size={28} className="text-amber" />,
      number: "98+",
      label: "Partner Charities",
      description: "Soup kitchens, orphanages & community kitchens",
      trend: "100% verified"
    },
    {
      id: 5,
      icon: <Leaf size={28} className="text-emerald" />,
      number: "60.8 Tons",
      label: "CO₂ Emissions Prevented",
      description: "Equivalent to planting 2,800 mature trees",
      trend: "Climate positive"
    }
  ];

  return (
    <section className="impact-section" id="impact">
      <div className="container">
        <div className="section-header text-center">
          <div className="badge badge-amber">
            <Award size={14} />
            <span>Live Food Rescue Telemetry</span>
          </div>
          <h2 className="section-title">Measurable Impact, Every Single Day</h2>
          <p className="section-subtitle">
            Every listing posted on RescuePlate contributes to fighting hunger, saving resources, 
            and combating climate change in real time.
          </p>
        </div>

        <div className="stats-grid">
          {stats.map((item) => (
            <div key={item.id} className="stat-card">
              <div className="stat-icon-wrapper">
                {item.icon}
              </div>
              <div className="stat-number">{item.number}</div>
              <div className="stat-label">{item.label}</div>
              <p className="stat-description">{item.description}</p>
              <div className="stat-trend-tag">
                <span className="trend-dot"></span> {item.trend}
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
