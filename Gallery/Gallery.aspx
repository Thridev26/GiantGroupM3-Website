<%@ Page Title="Our Work Gallery" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Gallery.aspx.cs" Inherits="GiantGroupM3.Gallery" %>
<asp:Content ID="GalleryContent" ContentPlaceHolderID="MainContent" runat="server">
<style>
 .work-hero{position:relative;overflow:hidden;background:linear-gradient(128deg,#0f2d1e,#1b4332 60%,#2d6a4f);border-radius:26px;padding:clamp(2rem,6vw,5.5rem);color:white;isolation:isolate}
 .work-hero:before{content:"";position:absolute;width:600px;height:600px;border:1px solid rgba(149,213,178,.2);border-radius:50%;right:-130px;top:-310px;box-shadow:0 0 0 65px rgba(149,213,178,.04),0 0 0 150px rgba(149,213,178,.04);z-index:-1}
 .work-kicker{font-size:.78rem;letter-spacing:.2em;text-transform:uppercase;color:#95d5b2;font-weight:700}
 .work-hero h1{font-family:var(--gg-display,Georgia,serif);font-size:clamp(2.6rem,6vw,5rem);max-width:850px;line-height:1.05;margin:1rem 0;color:#fff}
 .work-hero h1 em{color:#95d5b2;font-style:italic}
 .work-hero p{color:#d5eadd;max-width:660px;line-height:1.8;font-size:1.05rem}
 .work-section{padding:3rem 0 5rem}
 .work-top{display:flex;justify-content:space-between;align-items:flex-end;gap:1rem;flex-wrap:wrap;margin-bottom:1.5rem}
 .work-top h2{font-family:var(--gg-display,Georgia,serif);font-size:2rem;margin:0;color:var(--gg-text)}
 .work-filters{display:flex;flex-wrap:wrap;gap:.6rem}
 .work-filter{border:1px solid var(--gg-border);border-radius:99px;padding:.55rem 1rem;background:var(--gg-surface);color:var(--gg-text);font-size:.87rem;font-weight:600;cursor:pointer}
 .work-filter[aria-pressed="true"]{background:#2d6a4f;color:#fff;border-color:#2d6a4f}
 .work-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:1.6rem}
 .work-card{background:var(--gg-surface);border:1px solid var(--gg-border);border-radius:22px;overflow:hidden;box-shadow:var(--gg-shadow-sm);transition:transform .2s,box-shadow .2s}
 .work-card:hover{transform:translateY(-4px);box-shadow:var(--gg-shadow-md)}
 .work-comparison{position:relative;aspect-ratio:8/5;overflow:hidden;--split:50%;background:#163e2c;isolation:isolate}
 .work-comparison .work-after,.work-comparison .work-before{position:absolute;inset:0;width:100%;height:100%;object-fit:cover}
 .work-comparison .work-before{clip-path:inset(0 calc(100% - var(--split)) 0 0)}
 .work-tag{position:absolute;top:16px;z-index:3;background:rgba(15,45,30,.84);backdrop-filter:blur(8px);color:#fff;padding:.35rem .7rem;border-radius:7px;font-size:.75rem;font-weight:800;letter-spacing:.08em}
 .work-tag.before{left:16px}.work-tag.after{right:16px}
 .work-divider::after {
    content: "\2194";
    position: absolute;
    left: 50%;
    top: 50%;
    transform: translate(-50%, -50%);
    width: 43px;
    height: 43px;
    border-radius: 50%;
    background: white;
    color: #1b4332;
    display: grid;
    place-items: center;
    box-shadow: 0 4px 14px #0005;
    font-weight: 800;
    font-size: 22px;
}
 .work-range{position:absolute;z-index:4;inset:0;width:100%;height:100%;opacity:0;cursor:ew-resize;touch-action:pan-y}
 .work-copy{padding:1.45rem 1.5rem 1.65rem}
 .work-category{display:inline-flex;color:#2d6a4f;background:#e2f0e8;border-radius:6px;padding:.3rem .65rem;font-size:.75rem;font-weight:800;letter-spacing:.04em;text-transform:uppercase}
 .work-copy h3{font-family:var(--gg-display,Georgia,serif);font-size:1.45rem;margin:.8rem 0 .45rem;color:var(--gg-text)}
 .work-copy p{font-size:.9rem;color:var(--gg-muted);line-height:1.65;margin:0}
 .work-hint{font-size:.78rem;color:var(--gg-muted);margin-top:1rem;display:flex;align-items:center;gap:.45rem}
 .work-empty{padding:3.5rem 1.5rem;text-align:center;background:var(--gg-surface);border:1px dashed var(--gg-border);border-radius:20px;color:var(--gg-muted)}
 .work-cta{padding:2rem;background:var(--gg-accent-soft);border-radius:20px;text-align:center;margin-bottom:3rem}
 .work-cta h2{font-family:var(--gg-display,Georgia,serif);color:var(--gg-text)}
 @media(max-width:780px){.work-grid{grid-template-columns:1fr}.work-hero{border-radius:18px}.work-section{padding-top:2rem}}
 @media(prefers-reduced-motion:reduce){.work-card{transition:none}}
</style>
<div class="container py-4">
 <section class="work-hero">
  <div class="work-kicker">Giant Group / Our Transformations</div>
  <h1>Real work.<br/><em>Visible results.</em></h1>
  <p>Explore selected projects from our team. Move the slider across each image to see the transformation from before to after.</p>
 </section>
 <section class="work-section" aria-label="Our work gallery">
  <div class="work-top"><div><span class="gg-label">PROOF IN EVERY PROJECT</span><h2>Before &amp; after</h2></div><div id="galleryFilters" class="work-filters" aria-label="Filter by service"><button type="button" class="work-filter" data-filter="all" aria-pressed="true">All projects</button></div></div>
  <asp:Panel ID="EmptyPanel" runat="server" Visible="false" CssClass="work-empty"><i class="bi bi-images" style="font-size:2rem"></i><h3>Our gallery is coming together</h3><p>Check back soon for our latest completed projects.</p></asp:Panel>
  <div class="work-grid" id="galleryGrid">
   <asp:Repeater ID="GalleryRepeater" runat="server"><ItemTemplate>
    <article class="work-card" data-category='<%# Server.HtmlEncode(Convert.ToString(Eval("Category"))) %>'>
     <div class="work-comparison" style="--split:50%">
      <img class="work-after" loading="lazy" alt='<%# Server.HtmlEncode("After: " + Convert.ToString(Eval("Title"))) %>' src='<%# ResolveUrl("~/Gallery/GalleryImage.ashx?id=" + Eval("GalleryID") + "&side=after") %>' />
      <img class="work-before" loading="lazy" alt='<%# Server.HtmlEncode("Before: " + Convert.ToString(Eval("Title"))) %>' src='<%# ResolveUrl("~/Gallery/GalleryImage.ashx?id=" + Eval("GalleryID") + "&side=before") %>' />
      <span class="work-tag before">BEFORE</span><span class="work-tag after">AFTER</span>
      <div class="work-divider"></div>
      <input class="work-range" type="range" min="0" max="100" value="50" aria-label='<%# Server.HtmlEncode("Compare before and after: " + Convert.ToString(Eval("Title"))) %>' />
     </div>
     <div class="work-copy"><span class="work-category"><%#: Eval("Category") %></span><h3><%#: Eval("Title") %></h3><p><%#: Eval("Description") %></p><div class="work-hint"><i class="bi bi-arrows-expand"></i>Drag or use arrow keys to compare</div></div>
    </article>
   </ItemTemplate></asp:Repeater>
  </div>
 </section>
 <aside class="work-cta"><h2>Have a site that needs a transformation?</h2><p>Tell us about your project and we'll help plan the next step.</p><a runat="server" href="~/Contact.aspx" class="gg-btn gg-btn-primary">Get in touch</a></aside>
</div>
<script>
(function(){
 document.querySelectorAll('.work-range').forEach(function(slider){slider.addEventListener('input',function(){slider.parentElement.style.setProperty('--split',slider.value+'%');});});
 var filters=document.getElementById('galleryFilters'); if(!filters)return;
 var cards=Array.prototype.slice.call(document.querySelectorAll('.work-card'));
 var categories=[];cards.forEach(function(c){var v=c.getAttribute('data-category');if(v&&categories.indexOf(v)<0)categories.push(v);});
 categories.sort().forEach(function(v){var b=document.createElement('button');b.type='button';b.className='work-filter';b.textContent=v;b.setAttribute('aria-pressed','false');b.dataset.filter=v;filters.appendChild(b);});
 filters.addEventListener('click',function(e){var b=e.target.closest('button[data-filter]');if(!b)return;filters.querySelectorAll('button').forEach(function(x){x.setAttribute('aria-pressed',x===b?'true':'false');});cards.forEach(function(c){c.hidden=b.dataset.filter!=='all'&&c.getAttribute('data-category')!==b.dataset.filter;});});
})();
</script>
</asp:Content>
