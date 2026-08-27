import React, { useState } from 'react';
import { Star, Quote, ChevronLeft, ChevronRight, Building, HeartHandshake } from 'lucide-react';

export default function TestimonialsSection() {
  const testimonials = [
    {
      id: 1,
      quote: "Before RescuePlate, disposing of evening buffet surplus broke our hearts. Now, within 15 minutes of posting, a local shelter requests the exact portions they need. The handover is effortless and dignified.",
      author: "Chef Marco Rossi",
      role: "Executive Chef",
      organization: "Grand Azure Hotel & Suites",
      type: "Food Donor",
      impact: "1,850+ Meals Donated",
      avatar: "👨‍🍳"
    },
    {
      id: 2,
      quote: "The partial fulfilment feature is a lifesaver. Because we have limited cold storage, being able to request 25 portions from a 100-portion donation means nothing gets wasted on our end either.",
      author: "Sarah Jenkins",
      role: "Operations Director",
      organization: "Hope Community Shelter",
      type: "Charity Partner",
      impact: "Serving 120 Guests Daily",
      avatar: "👩‍💼"
    },
    {
      id: 3,
      quote: "As an artisan bakery, fresh bread has a strict shelf-life. RescuePlate connects us directly with breakfast clubs so yesterday’s unsold sourdough nourishes schoolchildren the next morning.",
      author: "Elena Rostova",
      role: "Founder & Head Baker",
      organization: "Rustic Hearth Bakery",
      type: "Food Donor",
      impact: "940 kg Saved",
      avatar: "🥖"
    },
    {
      id: 4,
      quote: "The instant notification engine gives our volunteer drivers precise pickup time windows. It has transformed how our neighborhood food network operates with zero administrative friction.",
      author: "Marcus Vance",
      role: "Logistics Coordinator",
      organization: "Urban Grace Community Kitchen",
      type: "Charity Partner",
      impact: "3,200+ Meals Collected",
      avatar: "🤝"
    }
  ];

  const [currentIndex, setCurrentIndex] = useState(0);

  const nextTestimonial = () => {
    setCurrentIndex((prev) => (prev + 1) % testimonials.length);
  };

  const prevTestimonial = () => {
    setCurrentIndex((prev) => (prev - 1 + testimonials.length) % testimonials.length);
  };

  return (
    <section className="testimonials-section" id="testimonials">
      <div className="container">
        <div className="section-header text-center">
          <div className="badge badge-amber">
            <Quote size={14} />
            <span>Community Voices</span>
          </div>
          <h2 className="section-title">Stories From The Frontlines of Food Rescue</h2>
          <p className="section-subtitle">
            Hear how restaurants, bakeries, and community organizers are turning surplus into community strength.
          </p>
        </div>

        {/* Carousel / Card Deck */}
        <div className="testimonial-container">
          <div className="testimonial-card">
            <div className="test-top-bar">
              <div className="test-type-pill">
                {testimonials[currentIndex].type === 'Food Donor' ? (
                  <span className="badge badge-primary">
                    <Building size={12} /> {testimonials[currentIndex].type}
                  </span>
                ) : (
                  <span className="badge badge-amber">
                    <HeartHandshake size={12} /> {testimonials[currentIndex].type}
                  </span>
                )}
              </div>

              <div className="star-rating">
                {[...Array(5)].map((_, i) => (
                  <Star key={i} size={16} fill="#f59e0b" color="#f59e0b" />
                ))}
              </div>
            </div>

            <p className="test-quote-text">
              "{testimonials[currentIndex].quote}"
            </p>

            <div className="test-footer">
              <div className="test-author-group">
                <div className="test-avatar">{testimonials[currentIndex].avatar}</div>
                <div>
                  <div className="test-author-name">{testimonials[currentIndex].author}</div>
                  <div className="test-author-role">
                    {testimonials[currentIndex].role} • <strong>{testimonials[currentIndex].organization}</strong>
                  </div>
                </div>
              </div>

              <div className="test-impact-badge">
                <span className="text-emerald font-semibold">{testimonials[currentIndex].impact}</span>
              </div>
            </div>
          </div>

          {/* Carousel Navigation Controls */}
          <div className="carousel-controls">
            <button 
              onClick={prevTestimonial}
              className="carousel-btn"
              aria-label="Previous testimonial"
            >
              <ChevronLeft size={20} />
            </button>

            <div className="carousel-dots">
              {testimonials.map((_, i) => (
                <button
                  key={i}
                  onClick={() => setCurrentIndex(i)}
                  className={`dot ${currentIndex === i ? 'dot-active' : ''}`}
                  aria-label={`Go to testimonial ${i + 1}`}
                />
              ))}
            </div>

            <button 
              onClick={nextTestimonial}
              className="carousel-btn"
              aria-label="Next testimonial"
            >
              <ChevronRight size={20} />
            </button>
          </div>
        </div>
      </div>
    </section>
  );
}
