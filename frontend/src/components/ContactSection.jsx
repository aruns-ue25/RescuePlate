import React, { useState } from 'react';
import { 
  Mail, 
  Phone, 
  MapPin, 
  Send, 
  HelpCircle, 
  ChevronDown, 
  ChevronUp, 
  CheckCircle2,
  Clock,
  Sparkles
} from 'lucide-react';

export default function ContactSection({ onShowToast }) {
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    role: 'DONOR',
    subject: '',
    message: ''
  });

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [openFaq, setOpenFaq] = useState(0);

  const faqs = [
    {
      q: "What food safety and liability standards apply to donors?",
      a: "RescuePlate enforces strict food handling, packaging, and temperature guidelines. Donors provide clear preparation timestamps and ingredient/allergen disclosures. In accordance with food donation protections and Good Samaritan principles, certified surplus food provided in good faith is legally safeguarded."
    },
    {
      q: "How does the Partial Fulfilment feature work?",
      a: "When a donor posts a large quantity (e.g. 100 meals), a charity that only needs 30 meals can request just that amount. The system automatically adjusts remaining inventory to 70 meals, allowing other community organizations to request the balance until fully claimed."
    },
    {
      q: "Is RescuePlate free for charities and community groups?",
      a: "Yes! 100% of food discovery, requests, and collection workflows are completely free of charge for non-profit organizations, shelters, soup kitchens, and community pantries."
    },
    {
      q: "What happens if a donation is not claimed before its availability deadline?",
      a: "RescuePlate incorporates automated expiry timers. If food reaches its safety deadline without being claimed, it automatically shifts to Expired status and cannot receive late requests, ensuring recipients only receive safe, fresh food."
    }
  ];

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!formData.name || !formData.email || !formData.message) {
      if (onShowToast) onShowToast('Please fill in all required fields.', 'error');
      return;
    }

    setIsSubmitting(true);
    setTimeout(() => {
      setIsSubmitting(false);
      setSubmitted(true);
      if (onShowToast) onShowToast('Thank you! Your message has been sent to the RescuePlate team.', 'success');
      setFormData({
        name: '',
        email: '',
        role: 'DONOR',
        subject: '',
        message: ''
      });
    }, 800);
  };

  return (
    <section className="contact-section" id="contact">
      <div className="container">
        <div className="section-header text-center">
          <div className="badge badge-primary">
            <Mail size={14} />
            <span>Connect & Inquire</span>
          </div>
          <h2 className="section-title">Get in Touch with Our Team</h2>
          <p className="section-subtitle">
            Have questions about onboarding your restaurant or registering your charity? 
            We're here to help you get started immediately.
          </p>
        </div>

        <div className="contact-grid">
          {/* Left: Contact Info & Interactive Form */}
          <div className="contact-form-card">
            <h3 className="form-card-title">Send Us a Message</h3>
            <p className="form-card-subtitle">Fill out the form below and our regional coordinator will reply within 24 hours.</p>

            {submitted ? (
              <div className="contact-success-state animate-fade-in-up">
                <div className="success-icon-box">
                  <CheckCircle2 size={42} className="text-emerald" />
                </div>
                <h4>Message Received!</h4>
                <p>Thank you for reaching out. We will review your inquiry and follow up shortly.</p>
                <button
                  onClick={() => setSubmitted(false)}
                  className="btn btn-outline btn-sm"
                >
                  Send Another Message
                </button>
              </div>
            ) : (
              <form onSubmit={handleSubmit} className="contact-form">
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">Your Name *</label>
                    <input
                      type="text"
                      className="form-input"
                      placeholder="e.g. Elena Rostova"
                      value={formData.name}
                      onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                      required
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">Email Address *</label>
                    <input
                      type="email"
                      className="form-input"
                      placeholder="name@business.com"
                      value={formData.email}
                      onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                      required
                    />
                  </div>
                </div>

                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">I Represent:</label>
                    <select
                      className="form-input"
                      value={formData.role}
                      onChange={(e) => setFormData({ ...formData, role: e.target.value })}
                    >
                      <option value="DONOR">Food Business / Donor</option>
                      <option value="ORGANIZATION">Charity / Nonprofit / Shelter</option>
                      <option value="COMMUNITY">Volunteer / Community Advocate</option>
                      <option value="MEDIA">Press / Media / Partner</option>
                    </select>
                  </div>

                  <div className="form-group">
                    <label className="form-label">Subject</label>
                    <input
                      type="text"
                      className="form-input"
                      placeholder="e.g. Partnership onboarding"
                      value={formData.subject}
                      onChange={(e) => setFormData({ ...formData, subject: e.target.value })}
                    />
                  </div>
                </div>

                <div className="form-group">
                  <label className="form-label">Message *</label>
                  <textarea
                    rows={4}
                    className="form-input form-textarea"
                    placeholder="Tell us how we can help you redistribute surplus or answer your questions..."
                    value={formData.message}
                    onChange={(e) => setFormData({ ...formData, message: e.target.value })}
                    required
                  ></textarea>
                </div>

                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="btn btn-primary btn-lg full-width"
                >
                  {isSubmitting ? (
                    <span>Sending Message...</span>
                  ) : (
                    <>
                      <Send size={18} />
                      <span>Send Inquiry</span>
                    </>
                  )}
                </button>
              </form>
            )}

            {/* Quick Contact Info Strip */}
            <div className="contact-info-strip">
              <div className="info-item">
                <Mail size={16} className="text-emerald" />
                <span>support@rescueplate.org</span>
              </div>
              <div className="info-item">
                <Phone size={16} className="text-amber" />
                <span>+1 (800) RESCUE-FOOD</span>
              </div>
              <div className="info-item">
                <Clock size={16} className="text-emerald" />
                <span>24/7 Rapid Response</span>
              </div>
            </div>
          </div>

          {/* Right: FAQ Accordion */}
          <div className="faq-wrapper">
            <div className="faq-header-box">
              <HelpCircle className="text-amber" size={24} />
              <h3>Frequently Asked Questions</h3>
            </div>

            <div className="faq-list">
              {faqs.map((faq, index) => (
                <div 
                  key={index} 
                  className={`faq-item ${openFaq === index ? 'faq-item-open' : ''}`}
                >
                  <button
                    className="faq-question-btn"
                    onClick={() => setOpenFaq(openFaq === index ? -1 : index)}
                    aria-expanded={openFaq === index}
                  >
                    <span>{faq.q}</span>
                    {openFaq === index ? <ChevronUp size={18} /> : <ChevronDown size={18} />}
                  </button>
                  {openFaq === index && (
                    <div className="faq-answer-content animate-fade-in-up">
                      <p>{faq.a}</p>
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
