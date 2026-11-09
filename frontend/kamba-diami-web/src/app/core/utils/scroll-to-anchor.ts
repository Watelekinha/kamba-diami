export function scrollToAnchor(elementId: string): void {
  const element = document.getElementById(elementId);
  if (!element) {
    return;
  }

  const navbar = document.querySelector('.navbar');
  const offset = (navbar instanceof HTMLElement ? navbar.offsetHeight : 72) + 8;
  const top = window.scrollY + element.getBoundingClientRect().top - offset;
  window.scrollTo({ top: Math.max(0, top), behavior: 'smooth' });
}
