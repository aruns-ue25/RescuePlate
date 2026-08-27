import React, { useState } from 'react';
import { 
  Utensils, 
  Croissant, 
  Apple, 
  Milk, 
  Package, 
  Coffee,
  CheckCircle2,
  ArrowRight
} from 'lucide-react';

export default function CategoriesSection({ onOpenAuth }) {
  const [selectedCat, setSelectedCat] = useState(0);

  const categories = [
    {
      id: 0,
      title: "Cooked & Prepared Meals",
      icon: <Utensils size={28} className="text-emerald" />,
      emoji: "🍲",
      badge: "High Priority",
      badgeColor: "badge-accent",
      activeListings: "28 active listings",
      description: "Hot entrees, surplus catering trays, gourmet hotel dishes, and freshly prepared meal boxes ready for immediate redistribution.",
      avgLife: "Safe 4-8 hr window",
      suitableFor: "Soup kitchens, homeless shelters, youth centers"
    },
    {
      id: 1,
      title: "Artisan Bakery & Pastries",
      icon: <Croissant size={28} className="text-amber" />,
      emoji: "🥐",
      badge: "High Volume",
      badgeColor: "badge-amber",
      activeListings: "45 active listings",
      description: "Daily baked artisanal sourdough, loaves, croissants, bagels, and pastries from top neighborhood bakeries.",
      avgLife: "12-24 hr window",
      suitableFor: "Community breakfast clubs, food pantries"
    },
    {
      id: 2,
      title: "Fresh Farm Produce",
      icon: <Apple size={28} className="text-emerald" />,
      emoji: "🥗",
      badge: "Nutrient Rich",
      badgeColor: "badge-primary",
      activeListings: "32 active listings",
      description: "Surplus fresh vegetables, seasonal fruits, organic greens, and root vegetables from markets and wholesalers.",
      avgLife: "24-48 hr window",
      suitableFor: "Community cooking kitchens, families in need"
    },
    {
      id: 3,
      title: "Dairy & Chilled Goods",
      icon: <Milk size={28} className="text-amber" />,
      emoji: "🥛",
      badge: "Cold-Chain Maintained",
      badgeColor: "badge-amber",
      activeListings: "19 active listings",
      description: "Unopened dairy milk, plant-based milk alternatives, pasteurized cheese, and sealed yogurts within best-before dates.",
      avgLife: "Chilled storage",
      suitableFor: "Children centers, senior care pantries"
    },
    {
      id: 4,
      title: "Pantry & Dry Packaged",
      icon: <Package size={28} className="text-emerald" />,
      emoji: "📦",
      badge: "Long Shelf-Life",
      badgeColor: "badge-primary",
      activeListings: "54 active listings",
      description: "Canned goods, dry pasta, rice, lentils, cereals, breakfast oats, and sealed snacks ideal for food parcel assembly.",
      avgLife: "Extended shelf-life",
      suitableFor: "Emergency relief boxes, weekend food drives"
    }
  ];

  return (
    <section className="categories-section" id="categories">
      <div className="container">
        <div className="section-header text-center">
          <div className="badge badge-primary">
            <span>Redistribution Categories</span>
          </div>
          <h2 className="section-title">What Types of Food Can You Rescue?</h2>
          <p className="section-subtitle">
            From hot chef-prepared meals to farm-fresh produce and pantry staples, 
            every category has a dedicated redistribution protocol.
          </p>
        </div>

        {/* Category Cards Grid */}
        <div className="categories-grid">
          {categories.map((cat, idx) => (
            <div 
              key={cat.id} 
              className={`category-card ${selectedCat === idx ? 'category-card-active' : ''}`}
              onClick={() => setSelectedCat(idx)}
            >
              <div className="category-header">
                <div className="category-icon-bubble">{cat.icon}</div>
                <span className={`badge ${cat.badgeColor}`}>{cat.badge}</span>
              </div>
              <h3 className="category-title">{cat.title}</h3>
              <p className="category-desc">{cat.description}</p>
              
              <div className="category-meta-box">
                <div className="meta-line">
                  <CheckCircle2 size={14} className="text-emerald" />
                  <span><strong>Ideal for:</strong> {cat.suitableFor}</span>
                </div>
                <div className="meta-line">
                  <span className="live-dot-green"></span>
                  <span className="text-emerald font-semibold">{cat.activeListings}</span>
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Bottom CTA */}
        <div className="categories-footer text-center">
          <button
            onClick={() => onOpenAuth('register', 'DONOR')}
            className="btn btn-primary btn-lg"
          >
            <span>Have Surplus Food to Donate? Post a Listing Now</span>
            <ArrowRight size={18} />
          </button>
        </div>
      </div>
    </section>
  );
}
