import React from 'react';
import { UserPlus, KeyRound, LayoutDashboard, ArrowRight } from 'lucide-react';
import { Link } from 'react-router-dom';

export default function HowItWorks() {
  const steps = [
    {
      num: "1",
      icon: <UserPlus size={24} className="text-emerald" />,
      title: "Register Account",
      desc: "Sign up as a Food Donor (restaurant, bakery, hotel) or a certified Charity Organization."
    },
    {
      num: "2",
      icon: <KeyRound size={24} className="text-amber" />,
      title: "Secure Authentication",
      desc: "Log in with your credentials to receive a verified JWT session and role-based portal access."
    },
    {
      num: "3",
      icon: <LayoutDashboard size={24} className="text-emerald" />,
      title: "Manage Profile & Portals",
      desc: "View and edit your business profile details, contact information, and food preferences."
    }
  ];

  return (
    <section className="how-it-works-section" id="how-it-works" style={{ padding: '60px 0 80px 0' }}>
      <div className="container" style={{ maxWidth: '900px', margin: '0 auto' }}>
        <div className="section-header text-center" style={{ marginBottom: '40px' }}>
          <h2 className="section-title" style={{ fontSize: '2rem' }}>How It Works</h2>
          <p className="section-subtitle" style={{ color: 'var(--text-muted)' }}>
            Get started with RescuePlate in 3 simple steps.
          </p>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '24px', marginBottom: '48px' }}>
          {steps.map((s) => (
            <div key={s.num} style={{ background: 'var(--bg-alt)', padding: '28px 20px', borderRadius: '16px', border: '1px solid var(--border)', textAlign: 'center' }}>
              <div style={{ width: '48px', height: '48px', borderRadius: '12px', background: '#ffffff', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 16px', border: '1px solid var(--border)' }}>
                {s.icon}
              </div>
              <h3 style={{ fontSize: '1.1rem', marginBottom: '8px' }}>{s.title}</h3>
              <p style={{ fontSize: '0.88rem', color: 'var(--text-muted)', lineHeight: '1.5' }}>{s.desc}</p>
            </div>
          ))}
        </div>

        <div style={{ textAlign: 'center' }}>
          <Link to="/register" className="btn btn-primary btn-lg">
            <span>Get Started Now</span>
            <ArrowRight size={18} />
          </Link>
        </div>
      </div>
    </section>
  );
}
