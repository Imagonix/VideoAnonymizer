/**
* @vue/shared v3.5.32
* (c) 2018-present Yuxi (Evan) You and Vue contributors
* @license MIT
**/
// @__NO_SIDE_EFFECTS__
function Ms(e) {
  const t = /* @__PURE__ */ Object.create(null);
  for (const s of e.split(",")) t[s] = 1;
  return (s) => s in t;
}
const k = {}, tt = [], Ee = () => {
}, Fn = () => !1, Xt = (e) => e.charCodeAt(0) === 111 && e.charCodeAt(1) === 110 && // uppercase letter
(e.charCodeAt(2) > 122 || e.charCodeAt(2) < 97), Zt = (e) => e.startsWith("onUpdate:"), Z = Object.assign, Rs = (e, t) => {
  const s = e.indexOf(t);
  s > -1 && e.splice(s, 1);
}, Ur = Object.prototype.hasOwnProperty, L = (e, t) => Ur.call(e, t), R = Array.isArray, st = (e) => It(e) === "[object Map]", jn = (e) => It(e) === "[object Set]", tn = (e) => It(e) === "[object Date]", j = (e) => typeof e == "function", Y = (e) => typeof e == "string", Ae = (e) => typeof e == "symbol", U = (e) => e !== null && typeof e == "object", Dn = (e) => (U(e) || j(e)) && j(e.then) && j(e.catch), $n = Object.prototype.toString, It = (e) => $n.call(e), Vr = (e) => It(e).slice(8, -1), Hn = (e) => It(e) === "[object Object]", Fs = (e) => Y(e) && e !== "NaN" && e[0] !== "-" && "" + parseInt(e, 10) === e, mt = /* @__PURE__ */ Ms(
  // the leading comma is intentional so empty string "" is also included
  ",key,ref,ref_for,ref_key,onVnodeBeforeMount,onVnodeMounted,onVnodeBeforeUpdate,onVnodeUpdated,onVnodeBeforeUnmount,onVnodeUnmounted"
), Qt = (e) => {
  const t = /* @__PURE__ */ Object.create(null);
  return (s) => t[s] || (t[s] = e(s));
}, Kr = /-\w/g, he = Qt(
  (e) => e.replace(Kr, (t) => t.slice(1).toUpperCase())
), Wr = /\B([A-Z])/g, Ze = Qt(
  (e) => e.replace(Wr, "-$1").toLowerCase()
), Nn = Qt((e) => e.charAt(0).toUpperCase() + e.slice(1)), fs = Qt(
  (e) => e ? `on${Nn(e)}` : ""
), Te = (e, t) => !Object.is(e, t), us = (e, ...t) => {
  for (let s = 0; s < e.length; s++)
    e[s](...t);
}, Ln = (e, t, s, n = !1) => {
  Object.defineProperty(e, t, {
    configurable: !0,
    enumerable: !1,
    writable: n,
    value: s
  });
}, Br = (e) => {
  const t = parseFloat(e);
  return isNaN(t) ? e : t;
};
let sn;
const es = () => sn || (sn = typeof globalThis < "u" ? globalThis : typeof self < "u" ? self : typeof window < "u" ? window : typeof global < "u" ? global : {});
function Ve(e) {
  if (R(e)) {
    const t = {};
    for (let s = 0; s < e.length; s++) {
      const n = e[s], r = Y(n) ? Jr(n) : Ve(n);
      if (r)
        for (const i in r)
          t[i] = r[i];
    }
    return t;
  } else if (Y(e) || U(e))
    return e;
}
const kr = /;(?![^(]*\))/g, qr = /:([^]+)/, Gr = /\/\*[^]*?\*\//g;
function Jr(e) {
  const t = {};
  return e.replace(Gr, "").split(kr).forEach((s) => {
    if (s) {
      const n = s.split(qr);
      n.length > 1 && (t[n[0].trim()] = n[1].trim());
    }
  }), t;
}
function js(e) {
  let t = "";
  if (Y(e))
    t = e;
  else if (R(e))
    for (let s = 0; s < e.length; s++) {
      const n = js(e[s]);
      n && (t += n + " ");
    }
  else if (U(e))
    for (const s in e)
      e[s] && (t += s + " ");
  return t.trim();
}
const Yr = "itemscope,allowfullscreen,formnovalidate,ismap,nomodule,novalidate,readonly", zr = /* @__PURE__ */ Ms(Yr);
function Un(e) {
  return !!e || e === "";
}
function Xr(e, t) {
  if (e.length !== t.length) return !1;
  let s = !0;
  for (let n = 0; s && n < e.length; n++)
    s = Ds(e[n], t[n]);
  return s;
}
function Ds(e, t) {
  if (e === t) return !0;
  let s = tn(e), n = tn(t);
  if (s || n)
    return s && n ? e.getTime() === t.getTime() : !1;
  if (s = Ae(e), n = Ae(t), s || n)
    return e === t;
  if (s = R(e), n = R(t), s || n)
    return s && n ? Xr(e, t) : !1;
  if (s = U(e), n = U(t), s || n) {
    if (!s || !n)
      return !1;
    const r = Object.keys(e).length, i = Object.keys(t).length;
    if (r !== i)
      return !1;
    for (const o in e) {
      const l = e.hasOwnProperty(o), f = t.hasOwnProperty(o);
      if (l && !f || !l && f || !Ds(e[o], t[o]))
        return !1;
    }
  }
  return String(e) === String(t);
}
const Vn = (e) => !!(e && e.__v_isRef === !0), Ut = (e) => Y(e) ? e : e == null ? "" : R(e) || U(e) && (e.toString === $n || !j(e.toString)) ? Vn(e) ? Ut(e.value) : JSON.stringify(e, Kn, 2) : String(e), Kn = (e, t) => Vn(t) ? Kn(e, t.value) : st(t) ? {
  [`Map(${t.size})`]: [...t.entries()].reduce(
    (s, [n, r], i) => (s[as(n, i) + " =>"] = r, s),
    {}
  )
} : jn(t) ? {
  [`Set(${t.size})`]: [...t.values()].map((s) => as(s))
} : Ae(t) ? as(t) : U(t) && !R(t) && !Hn(t) ? String(t) : t, as = (e, t = "") => {
  var s;
  return (
    // Symbol.description in es2019+ so we need to cast here to pass
    // the lib: es2016 check
    Ae(e) ? `Symbol(${(s = e.description) != null ? s : t})` : e
  );
};
/**
* @vue/reactivity v3.5.32
* (c) 2018-present Yuxi (Evan) You and Vue contributors
* @license MIT
**/
let oe;
class Zr {
  // TODO isolatedDeclarations "__v_skip"
  constructor(t = !1) {
    this.detached = t, this._active = !0, this._on = 0, this.effects = [], this.cleanups = [], this._isPaused = !1, this.__v_skip = !0, this.parent = oe, !t && oe && (this.index = (oe.scopes || (oe.scopes = [])).push(
      this
    ) - 1);
  }
  get active() {
    return this._active;
  }
  pause() {
    if (this._active) {
      this._isPaused = !0;
      let t, s;
      if (this.scopes)
        for (t = 0, s = this.scopes.length; t < s; t++)
          this.scopes[t].pause();
      for (t = 0, s = this.effects.length; t < s; t++)
        this.effects[t].pause();
    }
  }
  /**
   * Resumes the effect scope, including all child scopes and effects.
   */
  resume() {
    if (this._active && this._isPaused) {
      this._isPaused = !1;
      let t, s;
      if (this.scopes)
        for (t = 0, s = this.scopes.length; t < s; t++)
          this.scopes[t].resume();
      for (t = 0, s = this.effects.length; t < s; t++)
        this.effects[t].resume();
    }
  }
  run(t) {
    if (this._active) {
      const s = oe;
      try {
        return oe = this, t();
      } finally {
        oe = s;
      }
    }
  }
  /**
   * This should only be called on non-detached scopes
   * @internal
   */
  on() {
    ++this._on === 1 && (this.prevScope = oe, oe = this);
  }
  /**
   * This should only be called on non-detached scopes
   * @internal
   */
  off() {
    this._on > 0 && --this._on === 0 && (oe = this.prevScope, this.prevScope = void 0);
  }
  stop(t) {
    if (this._active) {
      this._active = !1;
      let s, n;
      for (s = 0, n = this.effects.length; s < n; s++)
        this.effects[s].stop();
      for (this.effects.length = 0, s = 0, n = this.cleanups.length; s < n; s++)
        this.cleanups[s]();
      if (this.cleanups.length = 0, this.scopes) {
        for (s = 0, n = this.scopes.length; s < n; s++)
          this.scopes[s].stop(!0);
        this.scopes.length = 0;
      }
      if (!this.detached && this.parent && !t) {
        const r = this.parent.scopes.pop();
        r && r !== this && (this.parent.scopes[this.index] = r, r.index = this.index);
      }
      this.parent = void 0;
    }
  }
}
function Qr() {
  return oe;
}
let B;
const ds = /* @__PURE__ */ new WeakSet();
class Wn {
  constructor(t) {
    this.fn = t, this.deps = void 0, this.depsTail = void 0, this.flags = 5, this.next = void 0, this.cleanup = void 0, this.scheduler = void 0, oe && oe.active && oe.effects.push(this);
  }
  pause() {
    this.flags |= 64;
  }
  resume() {
    this.flags & 64 && (this.flags &= -65, ds.has(this) && (ds.delete(this), this.trigger()));
  }
  /**
   * @internal
   */
  notify() {
    this.flags & 2 && !(this.flags & 32) || this.flags & 8 || kn(this);
  }
  run() {
    if (!(this.flags & 1))
      return this.fn();
    this.flags |= 2, nn(this), qn(this);
    const t = B, s = pe;
    B = this, pe = !0;
    try {
      return this.fn();
    } finally {
      Gn(this), B = t, pe = s, this.flags &= -3;
    }
  }
  stop() {
    if (this.flags & 1) {
      for (let t = this.deps; t; t = t.nextDep)
        Ns(t);
      this.deps = this.depsTail = void 0, nn(this), this.onStop && this.onStop(), this.flags &= -2;
    }
  }
  trigger() {
    this.flags & 64 ? ds.add(this) : this.scheduler ? this.scheduler() : this.runIfDirty();
  }
  /**
   * @internal
   */
  runIfDirty() {
    vs(this) && this.run();
  }
  get dirty() {
    return vs(this);
  }
}
let Bn = 0, bt, yt;
function kn(e, t = !1) {
  if (e.flags |= 8, t) {
    e.next = yt, yt = e;
    return;
  }
  e.next = bt, bt = e;
}
function $s() {
  Bn++;
}
function Hs() {
  if (--Bn > 0)
    return;
  if (yt) {
    let t = yt;
    for (yt = void 0; t; ) {
      const s = t.next;
      t.next = void 0, t.flags &= -9, t = s;
    }
  }
  let e;
  for (; bt; ) {
    let t = bt;
    for (bt = void 0; t; ) {
      const s = t.next;
      if (t.next = void 0, t.flags &= -9, t.flags & 1)
        try {
          t.trigger();
        } catch (n) {
          e || (e = n);
        }
      t = s;
    }
  }
  if (e) throw e;
}
function qn(e) {
  for (let t = e.deps; t; t = t.nextDep)
    t.version = -1, t.prevActiveLink = t.dep.activeLink, t.dep.activeLink = t;
}
function Gn(e) {
  let t, s = e.depsTail, n = s;
  for (; n; ) {
    const r = n.prevDep;
    n.version === -1 ? (n === s && (s = r), Ns(n), ei(n)) : t = n, n.dep.activeLink = n.prevActiveLink, n.prevActiveLink = void 0, n = r;
  }
  e.deps = t, e.depsTail = s;
}
function vs(e) {
  for (let t = e.deps; t; t = t.nextDep)
    if (t.dep.version !== t.version || t.dep.computed && (Jn(t.dep.computed) || t.dep.version !== t.version))
      return !0;
  return !!e._dirty;
}
function Jn(e) {
  if (e.flags & 4 && !(e.flags & 16) || (e.flags &= -17, e.globalVersion === Ct) || (e.globalVersion = Ct, !e.isSSR && e.flags & 128 && (!e.deps && !e._dirty || !vs(e))))
    return;
  e.flags |= 2;
  const t = e.dep, s = B, n = pe;
  B = e, pe = !0;
  try {
    qn(e);
    const r = e.fn(e._value);
    (t.version === 0 || Te(r, e._value)) && (e.flags |= 128, e._value = r, t.version++);
  } catch (r) {
    throw t.version++, r;
  } finally {
    B = s, pe = n, Gn(e), e.flags &= -3;
  }
}
function Ns(e, t = !1) {
  const { dep: s, prevSub: n, nextSub: r } = e;
  if (n && (n.nextSub = r, e.prevSub = void 0), r && (r.prevSub = n, e.nextSub = void 0), s.subs === e && (s.subs = n, !n && s.computed)) {
    s.computed.flags &= -5;
    for (let i = s.computed.deps; i; i = i.nextDep)
      Ns(i, !0);
  }
  !t && !--s.sc && s.map && s.map.delete(s.key);
}
function ei(e) {
  const { prevDep: t, nextDep: s } = e;
  t && (t.nextDep = s, e.prevDep = void 0), s && (s.prevDep = t, e.nextDep = void 0);
}
let pe = !0;
const Yn = [];
function je() {
  Yn.push(pe), pe = !1;
}
function De() {
  const e = Yn.pop();
  pe = e === void 0 ? !0 : e;
}
function nn(e) {
  const { cleanup: t } = e;
  if (e.cleanup = void 0, t) {
    const s = B;
    B = void 0;
    try {
      t();
    } finally {
      B = s;
    }
  }
}
let Ct = 0;
class ti {
  constructor(t, s) {
    this.sub = t, this.dep = s, this.version = s.version, this.nextDep = this.prevDep = this.nextSub = this.prevSub = this.prevActiveLink = void 0;
  }
}
class Ls {
  // TODO isolatedDeclarations "__v_skip"
  constructor(t) {
    this.computed = t, this.version = 0, this.activeLink = void 0, this.subs = void 0, this.map = void 0, this.key = void 0, this.sc = 0, this.__v_skip = !0;
  }
  track(t) {
    if (!B || !pe || B === this.computed)
      return;
    let s = this.activeLink;
    if (s === void 0 || s.sub !== B)
      s = this.activeLink = new ti(B, this), B.deps ? (s.prevDep = B.depsTail, B.depsTail.nextDep = s, B.depsTail = s) : B.deps = B.depsTail = s, zn(s);
    else if (s.version === -1 && (s.version = this.version, s.nextDep)) {
      const n = s.nextDep;
      n.prevDep = s.prevDep, s.prevDep && (s.prevDep.nextDep = n), s.prevDep = B.depsTail, s.nextDep = void 0, B.depsTail.nextDep = s, B.depsTail = s, B.deps === s && (B.deps = n);
    }
    return s;
  }
  trigger(t) {
    this.version++, Ct++, this.notify(t);
  }
  notify(t) {
    $s();
    try {
      for (let s = this.subs; s; s = s.prevSub)
        s.sub.notify() && s.sub.dep.notify();
    } finally {
      Hs();
    }
  }
}
function zn(e) {
  if (e.dep.sc++, e.sub.flags & 4) {
    const t = e.dep.computed;
    if (t && !e.dep.subs) {
      t.flags |= 20;
      for (let n = t.deps; n; n = n.nextDep)
        zn(n);
    }
    const s = e.dep.subs;
    s !== e && (e.prevSub = s, s && (s.nextSub = e)), e.dep.subs = e;
  }
}
const xs = /* @__PURE__ */ new WeakMap(), Ye = /* @__PURE__ */ Symbol(
  ""
), Ss = /* @__PURE__ */ Symbol(
  ""
), Tt = /* @__PURE__ */ Symbol(
  ""
);
function Q(e, t, s) {
  if (pe && B) {
    let n = xs.get(e);
    n || xs.set(e, n = /* @__PURE__ */ new Map());
    let r = n.get(s);
    r || (n.set(s, r = new Ls()), r.map = n, r.key = s), r.track();
  }
}
function Fe(e, t, s, n, r, i) {
  const o = xs.get(e);
  if (!o) {
    Ct++;
    return;
  }
  const l = (f) => {
    f && f.trigger();
  };
  if ($s(), t === "clear")
    o.forEach(l);
  else {
    const f = R(e), d = f && Fs(s);
    if (f && s === "length") {
      const a = Number(n);
      o.forEach((p, T) => {
        (T === "length" || T === Tt || !Ae(T) && T >= a) && l(p);
      });
    } else
      switch ((s !== void 0 || o.has(void 0)) && l(o.get(s)), d && l(o.get(Tt)), t) {
        case "add":
          f ? d && l(o.get("length")) : (l(o.get(Ye)), st(e) && l(o.get(Ss)));
          break;
        case "delete":
          f || (l(o.get(Ye)), st(e) && l(o.get(Ss)));
          break;
        case "set":
          st(e) && l(o.get(Ye));
          break;
      }
  }
  Hs();
}
function Qe(e) {
  const t = /* @__PURE__ */ N(e);
  return t === e ? t : (Q(t, "iterate", Tt), /* @__PURE__ */ ue(e) ? t : t.map(ge));
}
function ts(e) {
  return Q(e = /* @__PURE__ */ N(e), "iterate", Tt), e;
}
function we(e, t) {
  return /* @__PURE__ */ $e(e) ? it(/* @__PURE__ */ ze(e) ? ge(t) : t) : ge(t);
}
const si = {
  __proto__: null,
  [Symbol.iterator]() {
    return hs(this, Symbol.iterator, (e) => we(this, e));
  },
  concat(...e) {
    return Qe(this).concat(
      ...e.map((t) => R(t) ? Qe(t) : t)
    );
  },
  entries() {
    return hs(this, "entries", (e) => (e[1] = we(this, e[1]), e));
  },
  every(e, t) {
    return Pe(this, "every", e, t, void 0, arguments);
  },
  filter(e, t) {
    return Pe(
      this,
      "filter",
      e,
      t,
      (s) => s.map((n) => we(this, n)),
      arguments
    );
  },
  find(e, t) {
    return Pe(
      this,
      "find",
      e,
      t,
      (s) => we(this, s),
      arguments
    );
  },
  findIndex(e, t) {
    return Pe(this, "findIndex", e, t, void 0, arguments);
  },
  findLast(e, t) {
    return Pe(
      this,
      "findLast",
      e,
      t,
      (s) => we(this, s),
      arguments
    );
  },
  findLastIndex(e, t) {
    return Pe(this, "findLastIndex", e, t, void 0, arguments);
  },
  // flat, flatMap could benefit from ARRAY_ITERATE but are not straight-forward to implement
  forEach(e, t) {
    return Pe(this, "forEach", e, t, void 0, arguments);
  },
  includes(...e) {
    return ps(this, "includes", e);
  },
  indexOf(...e) {
    return ps(this, "indexOf", e);
  },
  join(e) {
    return Qe(this).join(e);
  },
  // keys() iterator only reads `length`, no optimization required
  lastIndexOf(...e) {
    return ps(this, "lastIndexOf", e);
  },
  map(e, t) {
    return Pe(this, "map", e, t, void 0, arguments);
  },
  pop() {
    return dt(this, "pop");
  },
  push(...e) {
    return dt(this, "push", e);
  },
  reduce(e, ...t) {
    return rn(this, "reduce", e, t);
  },
  reduceRight(e, ...t) {
    return rn(this, "reduceRight", e, t);
  },
  shift() {
    return dt(this, "shift");
  },
  // slice could use ARRAY_ITERATE but also seems to beg for range tracking
  some(e, t) {
    return Pe(this, "some", e, t, void 0, arguments);
  },
  splice(...e) {
    return dt(this, "splice", e);
  },
  toReversed() {
    return Qe(this).toReversed();
  },
  toSorted(e) {
    return Qe(this).toSorted(e);
  },
  toSpliced(...e) {
    return Qe(this).toSpliced(...e);
  },
  unshift(...e) {
    return dt(this, "unshift", e);
  },
  values() {
    return hs(this, "values", (e) => we(this, e));
  }
};
function hs(e, t, s) {
  const n = ts(e), r = n[t]();
  return n !== e && !/* @__PURE__ */ ue(e) && (r._next = r.next, r.next = () => {
    const i = r._next();
    return i.done || (i.value = s(i.value)), i;
  }), r;
}
const ni = Array.prototype;
function Pe(e, t, s, n, r, i) {
  const o = ts(e), l = o !== e && !/* @__PURE__ */ ue(e), f = o[t];
  if (f !== ni[t]) {
    const p = f.apply(e, i);
    return l ? ge(p) : p;
  }
  let d = s;
  o !== e && (l ? d = function(p, T) {
    return s.call(this, we(e, p), T, e);
  } : s.length > 2 && (d = function(p, T) {
    return s.call(this, p, T, e);
  }));
  const a = f.call(o, d, n);
  return l && r ? r(a) : a;
}
function rn(e, t, s, n) {
  const r = ts(e), i = r !== e && !/* @__PURE__ */ ue(e);
  let o = s, l = !1;
  r !== e && (i ? (l = n.length === 0, o = function(d, a, p) {
    return l && (l = !1, d = we(e, d)), s.call(this, d, we(e, a), p, e);
  }) : s.length > 3 && (o = function(d, a, p) {
    return s.call(this, d, a, p, e);
  }));
  const f = r[t](o, ...n);
  return l ? we(e, f) : f;
}
function ps(e, t, s) {
  const n = /* @__PURE__ */ N(e);
  Q(n, "iterate", Tt);
  const r = n[t](...s);
  return (r === -1 || r === !1) && /* @__PURE__ */ Ks(s[0]) ? (s[0] = /* @__PURE__ */ N(s[0]), n[t](...s)) : r;
}
function dt(e, t, s = []) {
  je(), $s();
  const n = (/* @__PURE__ */ N(e))[t].apply(e, s);
  return Hs(), De(), n;
}
const ri = /* @__PURE__ */ Ms("__proto__,__v_isRef,__isVue"), Xn = new Set(
  /* @__PURE__ */ Object.getOwnPropertyNames(Symbol).filter((e) => e !== "arguments" && e !== "caller").map((e) => Symbol[e]).filter(Ae)
);
function ii(e) {
  Ae(e) || (e = String(e));
  const t = /* @__PURE__ */ N(this);
  return Q(t, "has", e), t.hasOwnProperty(e);
}
class Zn {
  constructor(t = !1, s = !1) {
    this._isReadonly = t, this._isShallow = s;
  }
  get(t, s, n) {
    if (s === "__v_skip") return t.__v_skip;
    const r = this._isReadonly, i = this._isShallow;
    if (s === "__v_isReactive")
      return !r;
    if (s === "__v_isReadonly")
      return r;
    if (s === "__v_isShallow")
      return i;
    if (s === "__v_raw")
      return n === (r ? i ? gi : sr : i ? tr : er).get(t) || // receiver is not the reactive proxy, but has the same prototype
      // this means the receiver is a user proxy of the reactive proxy
      Object.getPrototypeOf(t) === Object.getPrototypeOf(n) ? t : void 0;
    const o = R(t);
    if (!r) {
      let f;
      if (o && (f = si[s]))
        return f;
      if (s === "hasOwnProperty")
        return ii;
    }
    const l = Reflect.get(
      t,
      s,
      // if this is a proxy wrapping a ref, return methods using the raw ref
      // as receiver so that we don't have to call `toRaw` on the ref in all
      // its class methods
      /* @__PURE__ */ ee(t) ? t : n
    );
    if ((Ae(s) ? Xn.has(s) : ri(s)) || (r || Q(t, "get", s), i))
      return l;
    if (/* @__PURE__ */ ee(l)) {
      const f = o && Fs(s) ? l : l.value;
      return r && U(f) ? /* @__PURE__ */ Cs(f) : f;
    }
    return U(l) ? r ? /* @__PURE__ */ Cs(l) : /* @__PURE__ */ ss(l) : l;
  }
}
class Qn extends Zn {
  constructor(t = !1) {
    super(!1, t);
  }
  set(t, s, n, r) {
    let i = t[s];
    const o = R(t) && Fs(s);
    if (!this._isShallow) {
      const d = /* @__PURE__ */ $e(i);
      if (!/* @__PURE__ */ ue(n) && !/* @__PURE__ */ $e(n) && (i = /* @__PURE__ */ N(i), n = /* @__PURE__ */ N(n)), !o && /* @__PURE__ */ ee(i) && !/* @__PURE__ */ ee(n))
        return d || (i.value = n), !0;
    }
    const l = o ? Number(s) < t.length : L(t, s), f = Reflect.set(
      t,
      s,
      n,
      /* @__PURE__ */ ee(t) ? t : r
    );
    return t === /* @__PURE__ */ N(r) && (l ? Te(n, i) && Fe(t, "set", s, n) : Fe(t, "add", s, n)), f;
  }
  deleteProperty(t, s) {
    const n = L(t, s);
    t[s];
    const r = Reflect.deleteProperty(t, s);
    return r && n && Fe(t, "delete", s, void 0), r;
  }
  has(t, s) {
    const n = Reflect.has(t, s);
    return (!Ae(s) || !Xn.has(s)) && Q(t, "has", s), n;
  }
  ownKeys(t) {
    return Q(
      t,
      "iterate",
      R(t) ? "length" : Ye
    ), Reflect.ownKeys(t);
  }
}
class oi extends Zn {
  constructor(t = !1) {
    super(!0, t);
  }
  set(t, s) {
    return !0;
  }
  deleteProperty(t, s) {
    return !0;
  }
}
const li = /* @__PURE__ */ new Qn(), ci = /* @__PURE__ */ new oi(), fi = /* @__PURE__ */ new Qn(!0);
const ws = (e) => e, Ht = (e) => Reflect.getPrototypeOf(e);
function ui(e, t, s) {
  return function(...n) {
    const r = this.__v_raw, i = /* @__PURE__ */ N(r), o = st(i), l = e === "entries" || e === Symbol.iterator && o, f = e === "keys" && o, d = r[e](...n), a = s ? ws : t ? it : ge;
    return !t && Q(
      i,
      "iterate",
      f ? Ss : Ye
    ), Z(
      // inheriting all iterator properties
      Object.create(d),
      {
        // iterator protocol
        next() {
          const { value: p, done: T } = d.next();
          return T ? { value: p, done: T } : {
            value: l ? [a(p[0]), a(p[1])] : a(p),
            done: T
          };
        }
      }
    );
  };
}
function Nt(e) {
  return function(...t) {
    return e === "delete" ? !1 : e === "clear" ? void 0 : this;
  };
}
function ai(e, t) {
  const s = {
    get(r) {
      const i = this.__v_raw, o = /* @__PURE__ */ N(i), l = /* @__PURE__ */ N(r);
      e || (Te(r, l) && Q(o, "get", r), Q(o, "get", l));
      const { has: f } = Ht(o), d = t ? ws : e ? it : ge;
      if (f.call(o, r))
        return d(i.get(r));
      if (f.call(o, l))
        return d(i.get(l));
      i !== o && i.get(r);
    },
    get size() {
      const r = this.__v_raw;
      return !e && Q(/* @__PURE__ */ N(r), "iterate", Ye), r.size;
    },
    has(r) {
      const i = this.__v_raw, o = /* @__PURE__ */ N(i), l = /* @__PURE__ */ N(r);
      return e || (Te(r, l) && Q(o, "has", r), Q(o, "has", l)), r === l ? i.has(r) : i.has(r) || i.has(l);
    },
    forEach(r, i) {
      const o = this, l = o.__v_raw, f = /* @__PURE__ */ N(l), d = t ? ws : e ? it : ge;
      return !e && Q(f, "iterate", Ye), l.forEach((a, p) => r.call(i, d(a), d(p), o));
    }
  };
  return Z(
    s,
    e ? {
      add: Nt("add"),
      set: Nt("set"),
      delete: Nt("delete"),
      clear: Nt("clear")
    } : {
      add(r) {
        const i = /* @__PURE__ */ N(this), o = Ht(i), l = /* @__PURE__ */ N(r), f = !t && !/* @__PURE__ */ ue(r) && !/* @__PURE__ */ $e(r) ? l : r;
        return o.has.call(i, f) || Te(r, f) && o.has.call(i, r) || Te(l, f) && o.has.call(i, l) || (i.add(f), Fe(i, "add", f, f)), this;
      },
      set(r, i) {
        !t && !/* @__PURE__ */ ue(i) && !/* @__PURE__ */ $e(i) && (i = /* @__PURE__ */ N(i));
        const o = /* @__PURE__ */ N(this), { has: l, get: f } = Ht(o);
        let d = l.call(o, r);
        d || (r = /* @__PURE__ */ N(r), d = l.call(o, r));
        const a = f.call(o, r);
        return o.set(r, i), d ? Te(i, a) && Fe(o, "set", r, i) : Fe(o, "add", r, i), this;
      },
      delete(r) {
        const i = /* @__PURE__ */ N(this), { has: o, get: l } = Ht(i);
        let f = o.call(i, r);
        f || (r = /* @__PURE__ */ N(r), f = o.call(i, r)), l && l.call(i, r);
        const d = i.delete(r);
        return f && Fe(i, "delete", r, void 0), d;
      },
      clear() {
        const r = /* @__PURE__ */ N(this), i = r.size !== 0, o = r.clear();
        return i && Fe(
          r,
          "clear",
          void 0,
          void 0
        ), o;
      }
    }
  ), [
    "keys",
    "values",
    "entries",
    Symbol.iterator
  ].forEach((r) => {
    s[r] = ui(r, e, t);
  }), s;
}
function Us(e, t) {
  const s = ai(e, t);
  return (n, r, i) => r === "__v_isReactive" ? !e : r === "__v_isReadonly" ? e : r === "__v_raw" ? n : Reflect.get(
    L(s, r) && r in n ? s : n,
    r,
    i
  );
}
const di = {
  get: /* @__PURE__ */ Us(!1, !1)
}, hi = {
  get: /* @__PURE__ */ Us(!1, !0)
}, pi = {
  get: /* @__PURE__ */ Us(!0, !1)
};
const er = /* @__PURE__ */ new WeakMap(), tr = /* @__PURE__ */ new WeakMap(), sr = /* @__PURE__ */ new WeakMap(), gi = /* @__PURE__ */ new WeakMap();
function _i(e) {
  switch (e) {
    case "Object":
    case "Array":
      return 1;
    case "Map":
    case "Set":
    case "WeakMap":
    case "WeakSet":
      return 2;
    default:
      return 0;
  }
}
function mi(e) {
  return e.__v_skip || !Object.isExtensible(e) ? 0 : _i(Vr(e));
}
// @__NO_SIDE_EFFECTS__
function ss(e) {
  return /* @__PURE__ */ $e(e) ? e : Vs(
    e,
    !1,
    li,
    di,
    er
  );
}
// @__NO_SIDE_EFFECTS__
function bi(e) {
  return Vs(
    e,
    !1,
    fi,
    hi,
    tr
  );
}
// @__NO_SIDE_EFFECTS__
function Cs(e) {
  return Vs(
    e,
    !0,
    ci,
    pi,
    sr
  );
}
function Vs(e, t, s, n, r) {
  if (!U(e) || e.__v_raw && !(t && e.__v_isReactive))
    return e;
  const i = mi(e);
  if (i === 0)
    return e;
  const o = r.get(e);
  if (o)
    return o;
  const l = new Proxy(
    e,
    i === 2 ? n : s
  );
  return r.set(e, l), l;
}
// @__NO_SIDE_EFFECTS__
function ze(e) {
  return /* @__PURE__ */ $e(e) ? /* @__PURE__ */ ze(e.__v_raw) : !!(e && e.__v_isReactive);
}
// @__NO_SIDE_EFFECTS__
function $e(e) {
  return !!(e && e.__v_isReadonly);
}
// @__NO_SIDE_EFFECTS__
function ue(e) {
  return !!(e && e.__v_isShallow);
}
// @__NO_SIDE_EFFECTS__
function Ks(e) {
  return e ? !!e.__v_raw : !1;
}
// @__NO_SIDE_EFFECTS__
function N(e) {
  const t = e && e.__v_raw;
  return t ? /* @__PURE__ */ N(t) : e;
}
function yi(e) {
  return !L(e, "__v_skip") && Object.isExtensible(e) && Ln(e, "__v_skip", !0), e;
}
const ge = (e) => U(e) ? /* @__PURE__ */ ss(e) : e, it = (e) => U(e) ? /* @__PURE__ */ Cs(e) : e;
// @__NO_SIDE_EFFECTS__
function ee(e) {
  return e ? e.__v_isRef === !0 : !1;
}
// @__NO_SIDE_EFFECTS__
function on(e) {
  return vi(e, !1);
}
function vi(e, t) {
  return /* @__PURE__ */ ee(e) ? e : new xi(e, t);
}
class xi {
  constructor(t, s) {
    this.dep = new Ls(), this.__v_isRef = !0, this.__v_isShallow = !1, this._rawValue = s ? t : /* @__PURE__ */ N(t), this._value = s ? t : ge(t), this.__v_isShallow = s;
  }
  get value() {
    return this.dep.track(), this._value;
  }
  set value(t) {
    const s = this._rawValue, n = this.__v_isShallow || /* @__PURE__ */ ue(t) || /* @__PURE__ */ $e(t);
    t = n ? t : /* @__PURE__ */ N(t), Te(t, s) && (this._rawValue = t, this._value = n ? t : ge(t), this.dep.trigger());
  }
}
function Si(e) {
  return /* @__PURE__ */ ee(e) ? e.value : e;
}
const wi = {
  get: (e, t, s) => t === "__v_raw" ? e : Si(Reflect.get(e, t, s)),
  set: (e, t, s, n) => {
    const r = e[t];
    return /* @__PURE__ */ ee(r) && !/* @__PURE__ */ ee(s) ? (r.value = s, !0) : Reflect.set(e, t, s, n);
  }
};
function nr(e) {
  return /* @__PURE__ */ ze(e) ? e : new Proxy(e, wi);
}
class Ci {
  constructor(t, s, n) {
    this.fn = t, this.setter = s, this._value = void 0, this.dep = new Ls(this), this.__v_isRef = !0, this.deps = void 0, this.depsTail = void 0, this.flags = 16, this.globalVersion = Ct - 1, this.next = void 0, this.effect = this, this.__v_isReadonly = !s, this.isSSR = n;
  }
  /**
   * @internal
   */
  notify() {
    if (this.flags |= 16, !(this.flags & 8) && // avoid infinite self recursion
    B !== this)
      return kn(this, !0), !0;
  }
  get value() {
    const t = this.dep.track();
    return Jn(this), t && (t.version = this.dep.version), this._value;
  }
  set value(t) {
    this.setter && this.setter(t);
  }
}
// @__NO_SIDE_EFFECTS__
function Ti(e, t, s = !1) {
  let n, r;
  return j(e) ? n = e : (n = e.get, r = e.set), new Ci(n, r, s);
}
const Lt = {}, kt = /* @__PURE__ */ new WeakMap();
let Je;
function Oi(e, t = !1, s = Je) {
  if (s) {
    let n = kt.get(s);
    n || kt.set(s, n = []), n.push(e);
  }
}
function Ei(e, t, s = k) {
  const { immediate: n, deep: r, once: i, scheduler: o, augmentJob: l, call: f } = s, d = (M) => r ? M : /* @__PURE__ */ ue(M) || r === !1 || r === 0 ? Ke(M, 1) : Ke(M);
  let a, p, T, O, H = !1, v = !1;
  if (/* @__PURE__ */ ee(e) ? (p = () => e.value, H = /* @__PURE__ */ ue(e)) : /* @__PURE__ */ ze(e) ? (p = () => d(e), H = !0) : R(e) ? (v = !0, H = e.some((M) => /* @__PURE__ */ ze(M) || /* @__PURE__ */ ue(M)), p = () => e.map((M) => {
    if (/* @__PURE__ */ ee(M))
      return M.value;
    if (/* @__PURE__ */ ze(M))
      return d(M);
    if (j(M))
      return f ? f(M, 2) : M();
  })) : j(e) ? t ? p = f ? () => f(e, 2) : e : p = () => {
    if (T) {
      je();
      try {
        T();
      } finally {
        De();
      }
    }
    const M = Je;
    Je = a;
    try {
      return f ? f(e, 3, [O]) : e(O);
    } finally {
      Je = M;
    }
  } : p = Ee, t && r) {
    const M = p, z = r === !0 ? 1 / 0 : r;
    p = () => Ke(M(), z);
  }
  const A = Qr(), F = () => {
    a.stop(), A && A.active && Rs(A.effects, a);
  };
  if (i && t) {
    const M = t;
    t = (...z) => {
      M(...z), F();
    };
  }
  let C = v ? new Array(e.length).fill(Lt) : Lt;
  const D = (M) => {
    if (!(!(a.flags & 1) || !a.dirty && !M))
      if (t) {
        const z = a.run();
        if (r || H || (v ? z.some((Ne, _e) => Te(Ne, C[_e])) : Te(z, C))) {
          T && T();
          const Ne = Je;
          Je = a;
          try {
            const _e = [
              z,
              // pass undefined as the old value when it's changed for the first time
              C === Lt ? void 0 : v && C[0] === Lt ? [] : C,
              O
            ];
            C = z, f ? f(t, 3, _e) : (
              // @ts-expect-error
              t(..._e)
            );
          } finally {
            Je = Ne;
          }
        }
      } else
        a.run();
  };
  return l && l(D), a = new Wn(p), a.scheduler = o ? () => o(D, !1) : D, O = (M) => Oi(M, !1, a), T = a.onStop = () => {
    const M = kt.get(a);
    if (M) {
      if (f)
        f(M, 4);
      else
        for (const z of M) z();
      kt.delete(a);
    }
  }, t ? n ? D(!0) : C = a.run() : o ? o(D.bind(null, !0), !0) : a.run(), F.pause = a.pause.bind(a), F.resume = a.resume.bind(a), F.stop = F, F;
}
function Ke(e, t = 1 / 0, s) {
  if (t <= 0 || !U(e) || e.__v_skip || (s = s || /* @__PURE__ */ new Map(), (s.get(e) || 0) >= t))
    return e;
  if (s.set(e, t), t--, /* @__PURE__ */ ee(e))
    Ke(e.value, t, s);
  else if (R(e))
    for (let n = 0; n < e.length; n++)
      Ke(e[n], t, s);
  else if (jn(e) || st(e))
    e.forEach((n) => {
      Ke(n, t, s);
    });
  else if (Hn(e)) {
    for (const n in e)
      Ke(e[n], t, s);
    for (const n of Object.getOwnPropertySymbols(e))
      Object.prototype.propertyIsEnumerable.call(e, n) && Ke(e[n], t, s);
  }
  return e;
}
/**
* @vue/runtime-core v3.5.32
* (c) 2018-present Yuxi (Evan) You and Vue contributors
* @license MIT
**/
function Pt(e, t, s, n) {
  try {
    return n ? e(...n) : e();
  } catch (r) {
    ns(r, t, s);
  }
}
function Ie(e, t, s, n) {
  if (j(e)) {
    const r = Pt(e, t, s, n);
    return r && Dn(r) && r.catch((i) => {
      ns(i, t, s);
    }), r;
  }
  if (R(e)) {
    const r = [];
    for (let i = 0; i < e.length; i++)
      r.push(Ie(e[i], t, s, n));
    return r;
  }
}
function ns(e, t, s, n = !0) {
  const r = t ? t.vnode : null, { errorHandler: i, throwUnhandledErrorInProduction: o } = t && t.appContext.config || k;
  if (t) {
    let l = t.parent;
    const f = t.proxy, d = `https://vuejs.org/error-reference/#runtime-${s}`;
    for (; l; ) {
      const a = l.ec;
      if (a) {
        for (let p = 0; p < a.length; p++)
          if (a[p](e, f, d) === !1)
            return;
      }
      l = l.parent;
    }
    if (i) {
      je(), Pt(i, null, 10, [
        e,
        f,
        d
      ]), De();
      return;
    }
  }
  Ai(e, s, r, n, o);
}
function Ai(e, t, s, n = !0, r = !1) {
  if (r)
    throw e;
  console.error(e);
}
const ne = [];
let Se = -1;
const nt = [];
let Ue = null, et = 0;
const rr = /* @__PURE__ */ Promise.resolve();
let qt = null;
function Ii(e) {
  const t = qt || rr;
  return e ? t.then(this ? e.bind(this) : e) : t;
}
function Pi(e) {
  let t = Se + 1, s = ne.length;
  for (; t < s; ) {
    const n = t + s >>> 1, r = ne[n], i = Ot(r);
    i < e || i === e && r.flags & 2 ? t = n + 1 : s = n;
  }
  return t;
}
function Ws(e) {
  if (!(e.flags & 1)) {
    const t = Ot(e), s = ne[ne.length - 1];
    !s || // fast path when the job id is larger than the tail
    !(e.flags & 2) && t >= Ot(s) ? ne.push(e) : ne.splice(Pi(t), 0, e), e.flags |= 1, ir();
  }
}
function ir() {
  qt || (qt = rr.then(lr));
}
function Mi(e) {
  R(e) ? nt.push(...e) : Ue && e.id === -1 ? Ue.splice(et + 1, 0, e) : e.flags & 1 || (nt.push(e), e.flags |= 1), ir();
}
function ln(e, t, s = Se + 1) {
  for (; s < ne.length; s++) {
    const n = ne[s];
    if (n && n.flags & 2) {
      if (e && n.id !== e.uid)
        continue;
      ne.splice(s, 1), s--, n.flags & 4 && (n.flags &= -2), n(), n.flags & 4 || (n.flags &= -2);
    }
  }
}
function or(e) {
  if (nt.length) {
    const t = [...new Set(nt)].sort(
      (s, n) => Ot(s) - Ot(n)
    );
    if (nt.length = 0, Ue) {
      Ue.push(...t);
      return;
    }
    for (Ue = t, et = 0; et < Ue.length; et++) {
      const s = Ue[et];
      s.flags & 4 && (s.flags &= -2), s.flags & 8 || s(), s.flags &= -2;
    }
    Ue = null, et = 0;
  }
}
const Ot = (e) => e.id == null ? e.flags & 2 ? -1 : 1 / 0 : e.id;
function lr(e) {
  try {
    for (Se = 0; Se < ne.length; Se++) {
      const t = ne[Se];
      t && !(t.flags & 8) && (t.flags & 4 && (t.flags &= -2), Pt(
        t,
        t.i,
        t.i ? 15 : 14
      ), t.flags & 4 || (t.flags &= -2));
    }
  } finally {
    for (; Se < ne.length; Se++) {
      const t = ne[Se];
      t && (t.flags &= -2);
    }
    Se = -1, ne.length = 0, or(), qt = null, (ne.length || nt.length) && lr();
  }
}
let Oe = null, cr = null;
function Gt(e) {
  const t = Oe;
  return Oe = e, cr = e && e.type.__scopeId || null, t;
}
function Ri(e, t = Oe, s) {
  if (!t || e._n)
    return e;
  const n = (...r) => {
    n._d && bn(-1);
    const i = Gt(t);
    let o;
    try {
      o = e(...r);
    } finally {
      Gt(i), n._d && bn(1);
    }
    return o;
  };
  return n._n = !0, n._c = !0, n._d = !0, n;
}
function qe(e, t, s, n) {
  const r = e.dirs, i = t && t.dirs;
  for (let o = 0; o < r.length; o++) {
    const l = r[o];
    i && (l.oldValue = i[o].value);
    let f = l.dir[n];
    f && (je(), Ie(f, s, 8, [
      e.el,
      l,
      e,
      t
    ]), De());
  }
}
function Fi(e, t) {
  if (re) {
    let s = re.provides;
    const n = re.parent && re.parent.provides;
    n === s && (s = re.provides = Object.create(n)), s[e] = t;
  }
}
function Vt(e, t, s = !1) {
  const n = Do();
  if (n || rt) {
    let r = rt ? rt._context.provides : n ? n.parent == null || n.ce ? n.vnode.appContext && n.vnode.appContext.provides : n.parent.provides : void 0;
    if (r && e in r)
      return r[e];
    if (arguments.length > 1)
      return s && j(t) ? t.call(n && n.proxy) : t;
  }
}
const ji = /* @__PURE__ */ Symbol.for("v-scx"), Di = () => Vt(ji);
function Kt(e, t, s) {
  return fr(e, t, s);
}
function fr(e, t, s = k) {
  const { immediate: n, deep: r, flush: i, once: o } = s, l = Z({}, s), f = t && n || !t && i !== "post";
  let d;
  if (At) {
    if (i === "sync") {
      const O = Di();
      d = O.__watcherHandles || (O.__watcherHandles = []);
    } else if (!f) {
      const O = () => {
      };
      return O.stop = Ee, O.resume = Ee, O.pause = Ee, O;
    }
  }
  const a = re;
  l.call = (O, H, v) => Ie(O, a, H, v);
  let p = !1;
  i === "post" ? l.scheduler = (O) => {
    ie(O, a && a.suspense);
  } : i !== "sync" && (p = !0, l.scheduler = (O, H) => {
    H ? O() : Ws(O);
  }), l.augmentJob = (O) => {
    t && (O.flags |= 4), p && (O.flags |= 2, a && (O.id = a.uid, O.i = a));
  };
  const T = Ei(e, t, l);
  return At && (d ? d.push(T) : f && T()), T;
}
function $i(e, t, s) {
  const n = this.proxy, r = Y(e) ? e.includes(".") ? ur(n, e) : () => n[e] : e.bind(n, n);
  let i;
  j(t) ? i = t : (i = t.handler, s = t);
  const o = Mt(this), l = fr(r, i.bind(n), s);
  return o(), l;
}
function ur(e, t) {
  const s = t.split(".");
  return () => {
    let n = e;
    for (let r = 0; r < s.length && n; r++)
      n = n[s[r]];
    return n;
  };
}
const Hi = /* @__PURE__ */ Symbol("_vte"), Ni = (e) => e.__isTeleport, Li = /* @__PURE__ */ Symbol("_leaveCb");
function Bs(e, t) {
  e.shapeFlag & 6 && e.component ? (e.transition = t, Bs(e.component.subTree, t)) : e.shapeFlag & 128 ? (e.ssContent.transition = t.clone(e.ssContent), e.ssFallback.transition = t.clone(e.ssFallback)) : e.transition = t;
}
// @__NO_SIDE_EFFECTS__
function Ui(e, t) {
  return j(e) ? (
    // #8236: extend call and options.name access are considered side-effects
    // by Rollup, so we have to wrap it in a pure-annotated IIFE.
    Z({ name: e.name }, t, { setup: e })
  ) : e;
}
function ar(e) {
  e.ids = [e.ids[0] + e.ids[2]++ + "-", 0, 0];
}
function cn(e, t) {
  let s;
  return !!((s = Object.getOwnPropertyDescriptor(e, t)) && !s.configurable);
}
const Jt = /* @__PURE__ */ new WeakMap();
function vt(e, t, s, n, r = !1) {
  if (R(e)) {
    e.forEach(
      (v, A) => vt(
        v,
        t && (R(t) ? t[A] : t),
        s,
        n,
        r
      )
    );
    return;
  }
  if (xt(n) && !r) {
    n.shapeFlag & 512 && n.type.__asyncResolved && n.component.subTree.component && vt(e, t, s, n.component.subTree);
    return;
  }
  const i = n.shapeFlag & 4 ? Js(n.component) : n.el, o = r ? null : i, { i: l, r: f } = e, d = t && t.r, a = l.refs === k ? l.refs = {} : l.refs, p = l.setupState, T = /* @__PURE__ */ N(p), O = p === k ? Fn : (v) => cn(a, v) ? !1 : L(T, v), H = (v, A) => !(A && cn(a, A));
  if (d != null && d !== f) {
    if (fn(t), Y(d))
      a[d] = null, O(d) && (p[d] = null);
    else if (/* @__PURE__ */ ee(d)) {
      const v = t;
      H(d, v.k) && (d.value = null), v.k && (a[v.k] = null);
    }
  }
  if (j(f))
    Pt(f, l, 12, [o, a]);
  else {
    const v = Y(f), A = /* @__PURE__ */ ee(f);
    if (v || A) {
      const F = () => {
        if (e.f) {
          const C = v ? O(f) ? p[f] : a[f] : H() || !e.k ? f.value : a[e.k];
          if (r)
            R(C) && Rs(C, i);
          else if (R(C))
            C.includes(i) || C.push(i);
          else if (v)
            a[f] = [i], O(f) && (p[f] = a[f]);
          else {
            const D = [i];
            H(f, e.k) && (f.value = D), e.k && (a[e.k] = D);
          }
        } else v ? (a[f] = o, O(f) && (p[f] = o)) : A && (H(f, e.k) && (f.value = o), e.k && (a[e.k] = o));
      };
      if (o) {
        const C = () => {
          F(), Jt.delete(e);
        };
        C.id = -1, Jt.set(e, C), ie(C, s);
      } else
        fn(e), F();
    }
  }
}
function fn(e) {
  const t = Jt.get(e);
  t && (t.flags |= 8, Jt.delete(e));
}
es().requestIdleCallback;
es().cancelIdleCallback;
const xt = (e) => !!e.type.__asyncLoader, dr = (e) => e.type.__isKeepAlive;
function Vi(e, t) {
  hr(e, "a", t);
}
function Ki(e, t) {
  hr(e, "da", t);
}
function hr(e, t, s = re) {
  const n = e.__wdc || (e.__wdc = () => {
    let r = s;
    for (; r; ) {
      if (r.isDeactivated)
        return;
      r = r.parent;
    }
    return e();
  });
  if (rs(t, n, s), s) {
    let r = s.parent;
    for (; r && r.parent; )
      dr(r.parent.vnode) && Wi(n, t, s, r), r = r.parent;
  }
}
function Wi(e, t, s, n) {
  const r = rs(
    t,
    e,
    n,
    !0
    /* prepend */
  );
  pr(() => {
    Rs(n[t], r);
  }, s);
}
function rs(e, t, s = re, n = !1) {
  if (s) {
    const r = s[e] || (s[e] = []), i = t.__weh || (t.__weh = (...o) => {
      je();
      const l = Mt(s), f = Ie(t, s, e, o);
      return l(), De(), f;
    });
    return n ? r.unshift(i) : r.push(i), i;
  }
}
const He = (e) => (t, s = re) => {
  (!At || e === "sp") && rs(e, (...n) => t(...n), s);
}, Bi = He("bm"), ki = He("m"), qi = He(
  "bu"
), Gi = He("u"), Ji = He(
  "bum"
), pr = He("um"), Yi = He(
  "sp"
), zi = He("rtg"), Xi = He("rtc");
function Zi(e, t = re) {
  rs("ec", e, t);
}
const Qi = /* @__PURE__ */ Symbol.for("v-ndc");
function ht(e, t, s, n) {
  let r;
  const i = s, o = R(e);
  if (o || Y(e)) {
    const l = o && /* @__PURE__ */ ze(e);
    let f = !1, d = !1;
    l && (f = !/* @__PURE__ */ ue(e), d = /* @__PURE__ */ $e(e), e = ts(e)), r = new Array(e.length);
    for (let a = 0, p = e.length; a < p; a++)
      r[a] = t(
        f ? d ? it(ge(e[a])) : ge(e[a]) : e[a],
        a,
        void 0,
        i
      );
  } else if (typeof e == "number") {
    r = new Array(e);
    for (let l = 0; l < e; l++)
      r[l] = t(l + 1, l, void 0, i);
  } else if (U(e))
    if (e[Symbol.iterator])
      r = Array.from(
        e,
        (l, f) => t(l, f, void 0, i)
      );
    else {
      const l = Object.keys(e);
      r = new Array(l.length);
      for (let f = 0, d = l.length; f < d; f++) {
        const a = l[f];
        r[f] = t(e[a], a, f, i);
      }
    }
  else
    r = [];
  return r;
}
const Ts = (e) => e ? Dr(e) ? Js(e) : Ts(e.parent) : null, St = (
  // Move PURE marker to new line to workaround compiler discarding it
  // due to type annotation
  /* @__PURE__ */ Z(/* @__PURE__ */ Object.create(null), {
    $: (e) => e,
    $el: (e) => e.vnode.el,
    $data: (e) => e.data,
    $props: (e) => e.props,
    $attrs: (e) => e.attrs,
    $slots: (e) => e.slots,
    $refs: (e) => e.refs,
    $parent: (e) => Ts(e.parent),
    $root: (e) => Ts(e.root),
    $host: (e) => e.ce,
    $emit: (e) => e.emit,
    $options: (e) => _r(e),
    $forceUpdate: (e) => e.f || (e.f = () => {
      Ws(e.update);
    }),
    $nextTick: (e) => e.n || (e.n = Ii.bind(e.proxy)),
    $watch: (e) => $i.bind(e)
  })
), gs = (e, t) => e !== k && !e.__isScriptSetup && L(e, t), eo = {
  get({ _: e }, t) {
    if (t === "__v_skip")
      return !0;
    const { ctx: s, setupState: n, data: r, props: i, accessCache: o, type: l, appContext: f } = e;
    if (t[0] !== "$") {
      const T = o[t];
      if (T !== void 0)
        switch (T) {
          case 1:
            return n[t];
          case 2:
            return r[t];
          case 4:
            return s[t];
          case 3:
            return i[t];
        }
      else {
        if (gs(n, t))
          return o[t] = 1, n[t];
        if (r !== k && L(r, t))
          return o[t] = 2, r[t];
        if (L(i, t))
          return o[t] = 3, i[t];
        if (s !== k && L(s, t))
          return o[t] = 4, s[t];
        Os && (o[t] = 0);
      }
    }
    const d = St[t];
    let a, p;
    if (d)
      return t === "$attrs" && Q(e.attrs, "get", ""), d(e);
    if (
      // css module (injected by vue-loader)
      (a = l.__cssModules) && (a = a[t])
    )
      return a;
    if (s !== k && L(s, t))
      return o[t] = 4, s[t];
    if (
      // global properties
      p = f.config.globalProperties, L(p, t)
    )
      return p[t];
  },
  set({ _: e }, t, s) {
    const { data: n, setupState: r, ctx: i } = e;
    return gs(r, t) ? (r[t] = s, !0) : n !== k && L(n, t) ? (n[t] = s, !0) : L(e.props, t) || t[0] === "$" && t.slice(1) in e ? !1 : (i[t] = s, !0);
  },
  has({
    _: { data: e, setupState: t, accessCache: s, ctx: n, appContext: r, props: i, type: o }
  }, l) {
    let f;
    return !!(s[l] || e !== k && l[0] !== "$" && L(e, l) || gs(t, l) || L(i, l) || L(n, l) || L(St, l) || L(r.config.globalProperties, l) || (f = o.__cssModules) && f[l]);
  },
  defineProperty(e, t, s) {
    return s.get != null ? e._.accessCache[t] = 0 : L(s, "value") && this.set(e, t, s.value, null), Reflect.defineProperty(e, t, s);
  }
};
function un(e) {
  return R(e) ? e.reduce(
    (t, s) => (t[s] = null, t),
    {}
  ) : e;
}
let Os = !0;
function to(e) {
  const t = _r(e), s = e.proxy, n = e.ctx;
  Os = !1, t.beforeCreate && an(t.beforeCreate, e, "bc");
  const {
    // state
    data: r,
    computed: i,
    methods: o,
    watch: l,
    provide: f,
    inject: d,
    // lifecycle
    created: a,
    beforeMount: p,
    mounted: T,
    beforeUpdate: O,
    updated: H,
    activated: v,
    deactivated: A,
    beforeDestroy: F,
    beforeUnmount: C,
    destroyed: D,
    unmounted: M,
    render: z,
    renderTracked: Ne,
    renderTriggered: _e,
    errorCaptured: Le,
    serverPrefetch: Rt,
    // public API
    expose: We,
    inheritAttrs: ct,
    // assets
    components: Ft,
    directives: jt,
    filters: ls
  } = t;
  if (d && so(d, n, null), o)
    for (const q in o) {
      const K = o[q];
      j(K) && (n[q] = K.bind(s));
    }
  if (r) {
    const q = r.call(s, s);
    U(q) && (e.data = /* @__PURE__ */ ss(q));
  }
  if (Os = !0, i)
    for (const q in i) {
      const K = i[q], Be = j(K) ? K.bind(s, s) : j(K.get) ? K.get.bind(s, s) : Ee, Dt = !j(K) && j(K.set) ? K.set.bind(s) : Ee, ke = _t({
        get: Be,
        set: Dt
      });
      Object.defineProperty(n, q, {
        enumerable: !0,
        configurable: !0,
        get: () => ke.value,
        set: (me) => ke.value = me
      });
    }
  if (l)
    for (const q in l)
      gr(l[q], n, s, q);
  if (f) {
    const q = j(f) ? f.call(s) : f;
    Reflect.ownKeys(q).forEach((K) => {
      Fi(K, q[K]);
    });
  }
  a && an(a, e, "c");
  function te(q, K) {
    R(K) ? K.forEach((Be) => q(Be.bind(s))) : K && q(K.bind(s));
  }
  if (te(Bi, p), te(ki, T), te(qi, O), te(Gi, H), te(Vi, v), te(Ki, A), te(Zi, Le), te(Xi, Ne), te(zi, _e), te(Ji, C), te(pr, M), te(Yi, Rt), R(We))
    if (We.length) {
      const q = e.exposed || (e.exposed = {});
      We.forEach((K) => {
        Object.defineProperty(q, K, {
          get: () => s[K],
          set: (Be) => s[K] = Be,
          enumerable: !0
        });
      });
    } else e.exposed || (e.exposed = {});
  z && e.render === Ee && (e.render = z), ct != null && (e.inheritAttrs = ct), Ft && (e.components = Ft), jt && (e.directives = jt), Rt && ar(e);
}
function so(e, t, s = Ee) {
  R(e) && (e = Es(e));
  for (const n in e) {
    const r = e[n];
    let i;
    U(r) ? "default" in r ? i = Vt(
      r.from || n,
      r.default,
      !0
    ) : i = Vt(r.from || n) : i = Vt(r), /* @__PURE__ */ ee(i) ? Object.defineProperty(t, n, {
      enumerable: !0,
      configurable: !0,
      get: () => i.value,
      set: (o) => i.value = o
    }) : t[n] = i;
  }
}
function an(e, t, s) {
  Ie(
    R(e) ? e.map((n) => n.bind(t.proxy)) : e.bind(t.proxy),
    t,
    s
  );
}
function gr(e, t, s, n) {
  let r = n.includes(".") ? ur(s, n) : () => s[n];
  if (Y(e)) {
    const i = t[e];
    j(i) && Kt(r, i);
  } else if (j(e))
    Kt(r, e.bind(s));
  else if (U(e))
    if (R(e))
      e.forEach((i) => gr(i, t, s, n));
    else {
      const i = j(e.handler) ? e.handler.bind(s) : t[e.handler];
      j(i) && Kt(r, i, e);
    }
}
function _r(e) {
  const t = e.type, { mixins: s, extends: n } = t, {
    mixins: r,
    optionsCache: i,
    config: { optionMergeStrategies: o }
  } = e.appContext, l = i.get(t);
  let f;
  return l ? f = l : !r.length && !s && !n ? f = t : (f = {}, r.length && r.forEach(
    (d) => Yt(f, d, o, !0)
  ), Yt(f, t, o)), U(t) && i.set(t, f), f;
}
function Yt(e, t, s, n = !1) {
  const { mixins: r, extends: i } = t;
  i && Yt(e, i, s, !0), r && r.forEach(
    (o) => Yt(e, o, s, !0)
  );
  for (const o in t)
    if (!(n && o === "expose")) {
      const l = no[o] || s && s[o];
      e[o] = l ? l(e[o], t[o]) : t[o];
    }
  return e;
}
const no = {
  data: dn,
  props: hn,
  emits: hn,
  // objects
  methods: gt,
  computed: gt,
  // lifecycle
  beforeCreate: se,
  created: se,
  beforeMount: se,
  mounted: se,
  beforeUpdate: se,
  updated: se,
  beforeDestroy: se,
  beforeUnmount: se,
  destroyed: se,
  unmounted: se,
  activated: se,
  deactivated: se,
  errorCaptured: se,
  serverPrefetch: se,
  // assets
  components: gt,
  directives: gt,
  // watch
  watch: io,
  // provide / inject
  provide: dn,
  inject: ro
};
function dn(e, t) {
  return t ? e ? function() {
    return Z(
      j(e) ? e.call(this, this) : e,
      j(t) ? t.call(this, this) : t
    );
  } : t : e;
}
function ro(e, t) {
  return gt(Es(e), Es(t));
}
function Es(e) {
  if (R(e)) {
    const t = {};
    for (let s = 0; s < e.length; s++)
      t[e[s]] = e[s];
    return t;
  }
  return e;
}
function se(e, t) {
  return e ? [...new Set([].concat(e, t))] : t;
}
function gt(e, t) {
  return e ? Z(/* @__PURE__ */ Object.create(null), e, t) : t;
}
function hn(e, t) {
  return e ? R(e) && R(t) ? [.../* @__PURE__ */ new Set([...e, ...t])] : Z(
    /* @__PURE__ */ Object.create(null),
    un(e),
    un(t ?? {})
  ) : t;
}
function io(e, t) {
  if (!e) return t;
  if (!t) return e;
  const s = Z(/* @__PURE__ */ Object.create(null), e);
  for (const n in t)
    s[n] = se(e[n], t[n]);
  return s;
}
function mr() {
  return {
    app: null,
    config: {
      isNativeTag: Fn,
      performance: !1,
      globalProperties: {},
      optionMergeStrategies: {},
      errorHandler: void 0,
      warnHandler: void 0,
      compilerOptions: {}
    },
    mixins: [],
    components: {},
    directives: {},
    provides: /* @__PURE__ */ Object.create(null),
    optionsCache: /* @__PURE__ */ new WeakMap(),
    propsCache: /* @__PURE__ */ new WeakMap(),
    emitsCache: /* @__PURE__ */ new WeakMap()
  };
}
let oo = 0;
function lo(e, t) {
  return function(n, r = null) {
    j(n) || (n = Z({}, n)), r != null && !U(r) && (r = null);
    const i = mr(), o = /* @__PURE__ */ new WeakSet(), l = [];
    let f = !1;
    const d = i.app = {
      _uid: oo++,
      _component: n,
      _props: r,
      _container: null,
      _context: i,
      _instance: null,
      version: Vo,
      get config() {
        return i.config;
      },
      set config(a) {
      },
      use(a, ...p) {
        return o.has(a) || (a && j(a.install) ? (o.add(a), a.install(d, ...p)) : j(a) && (o.add(a), a(d, ...p))), d;
      },
      mixin(a) {
        return i.mixins.includes(a) || i.mixins.push(a), d;
      },
      component(a, p) {
        return p ? (i.components[a] = p, d) : i.components[a];
      },
      directive(a, p) {
        return p ? (i.directives[a] = p, d) : i.directives[a];
      },
      mount(a, p, T) {
        if (!f) {
          const O = d._ceVNode || Xe(n, r);
          return O.appContext = i, T === !0 ? T = "svg" : T === !1 && (T = void 0), e(O, a, T), f = !0, d._container = a, a.__vue_app__ = d, Js(O.component);
        }
      },
      onUnmount(a) {
        l.push(a);
      },
      unmount() {
        f && (Ie(
          l,
          d._instance,
          16
        ), e(null, d._container), delete d._container.__vue_app__);
      },
      provide(a, p) {
        return i.provides[a] = p, d;
      },
      runWithContext(a) {
        const p = rt;
        rt = d;
        try {
          return a();
        } finally {
          rt = p;
        }
      }
    };
    return d;
  };
}
let rt = null;
const co = (e, t) => t === "modelValue" || t === "model-value" ? e.modelModifiers : e[`${t}Modifiers`] || e[`${he(t)}Modifiers`] || e[`${Ze(t)}Modifiers`];
function fo(e, t, ...s) {
  if (e.isUnmounted) return;
  const n = e.vnode.props || k;
  let r = s;
  const i = t.startsWith("update:"), o = i && co(n, t.slice(7));
  o && (o.trim && (r = s.map((a) => Y(a) ? a.trim() : a)), o.number && (r = s.map(Br)));
  let l, f = n[l = fs(t)] || // also try camelCase event handler (#2249)
  n[l = fs(he(t))];
  !f && i && (f = n[l = fs(Ze(t))]), f && Ie(
    f,
    e,
    6,
    r
  );
  const d = n[l + "Once"];
  if (d) {
    if (!e.emitted)
      e.emitted = {};
    else if (e.emitted[l])
      return;
    e.emitted[l] = !0, Ie(
      d,
      e,
      6,
      r
    );
  }
}
const uo = /* @__PURE__ */ new WeakMap();
function br(e, t, s = !1) {
  const n = s ? uo : t.emitsCache, r = n.get(e);
  if (r !== void 0)
    return r;
  const i = e.emits;
  let o = {}, l = !1;
  if (!j(e)) {
    const f = (d) => {
      const a = br(d, t, !0);
      a && (l = !0, Z(o, a));
    };
    !s && t.mixins.length && t.mixins.forEach(f), e.extends && f(e.extends), e.mixins && e.mixins.forEach(f);
  }
  return !i && !l ? (U(e) && n.set(e, null), null) : (R(i) ? i.forEach((f) => o[f] = null) : Z(o, i), U(e) && n.set(e, o), o);
}
function is(e, t) {
  return !e || !Xt(t) ? !1 : (t = t.slice(2).replace(/Once$/, ""), L(e, t[0].toLowerCase() + t.slice(1)) || L(e, Ze(t)) || L(e, t));
}
function pn(e) {
  const {
    type: t,
    vnode: s,
    proxy: n,
    withProxy: r,
    propsOptions: [i],
    slots: o,
    attrs: l,
    emit: f,
    render: d,
    renderCache: a,
    props: p,
    data: T,
    setupState: O,
    ctx: H,
    inheritAttrs: v
  } = e, A = Gt(e);
  let F, C;
  try {
    if (s.shapeFlag & 4) {
      const M = r || n, z = M;
      F = Ce(
        d.call(
          z,
          M,
          a,
          p,
          O,
          T,
          H
        )
      ), C = l;
    } else {
      const M = t;
      F = Ce(
        M.length > 1 ? M(
          p,
          { attrs: l, slots: o, emit: f }
        ) : M(
          p,
          null
        )
      ), C = t.props ? l : ao(l);
    }
  } catch (M) {
    wt.length = 0, ns(M, e, 1), F = Xe(ot);
  }
  let D = F;
  if (C && v !== !1) {
    const M = Object.keys(C), { shapeFlag: z } = D;
    M.length && z & 7 && (i && M.some(Zt) && (C = ho(
      C,
      i
    )), D = lt(D, C, !1, !0));
  }
  return s.dirs && (D = lt(D, null, !1, !0), D.dirs = D.dirs ? D.dirs.concat(s.dirs) : s.dirs), s.transition && Bs(D, s.transition), F = D, Gt(A), F;
}
const ao = (e) => {
  let t;
  for (const s in e)
    (s === "class" || s === "style" || Xt(s)) && ((t || (t = {}))[s] = e[s]);
  return t;
}, ho = (e, t) => {
  const s = {};
  for (const n in e)
    (!Zt(n) || !(n.slice(9) in t)) && (s[n] = e[n]);
  return s;
};
function po(e, t, s) {
  const { props: n, children: r, component: i } = e, { props: o, children: l, patchFlag: f } = t, d = i.emitsOptions;
  if (t.dirs || t.transition)
    return !0;
  if (s && f >= 0) {
    if (f & 1024)
      return !0;
    if (f & 16)
      return n ? gn(n, o, d) : !!o;
    if (f & 8) {
      const a = t.dynamicProps;
      for (let p = 0; p < a.length; p++) {
        const T = a[p];
        if (yr(o, n, T) && !is(d, T))
          return !0;
      }
    }
  } else
    return (r || l) && (!l || !l.$stable) ? !0 : n === o ? !1 : n ? o ? gn(n, o, d) : !0 : !!o;
  return !1;
}
function gn(e, t, s) {
  const n = Object.keys(t);
  if (n.length !== Object.keys(e).length)
    return !0;
  for (let r = 0; r < n.length; r++) {
    const i = n[r];
    if (yr(t, e, i) && !is(s, i))
      return !0;
  }
  return !1;
}
function yr(e, t, s) {
  const n = e[s], r = t[s];
  return s === "style" && U(n) && U(r) ? !Ds(n, r) : n !== r;
}
function go({ vnode: e, parent: t, suspense: s }, n) {
  for (; t; ) {
    const r = t.subTree;
    if (r.suspense && r.suspense.activeBranch === e && (r.suspense.vnode.el = r.el = n, e = r), r === e)
      (e = t.vnode).el = n, t = t.parent;
    else
      break;
  }
  s && s.activeBranch === e && (s.vnode.el = n);
}
const vr = {}, xr = () => Object.create(vr), Sr = (e) => Object.getPrototypeOf(e) === vr;
function _o(e, t, s, n = !1) {
  const r = {}, i = xr();
  e.propsDefaults = /* @__PURE__ */ Object.create(null), wr(e, t, r, i);
  for (const o in e.propsOptions[0])
    o in r || (r[o] = void 0);
  s ? e.props = n ? r : /* @__PURE__ */ bi(r) : e.type.props ? e.props = r : e.props = i, e.attrs = i;
}
function mo(e, t, s, n) {
  const {
    props: r,
    attrs: i,
    vnode: { patchFlag: o }
  } = e, l = /* @__PURE__ */ N(r), [f] = e.propsOptions;
  let d = !1;
  if (
    // always force full diff in dev
    // - #1942 if hmr is enabled with sfc component
    // - vite#872 non-sfc component used by sfc component
    (n || o > 0) && !(o & 16)
  ) {
    if (o & 8) {
      const a = e.vnode.dynamicProps;
      for (let p = 0; p < a.length; p++) {
        let T = a[p];
        if (is(e.emitsOptions, T))
          continue;
        const O = t[T];
        if (f)
          if (L(i, T))
            O !== i[T] && (i[T] = O, d = !0);
          else {
            const H = he(T);
            r[H] = As(
              f,
              l,
              H,
              O,
              e,
              !1
            );
          }
        else
          O !== i[T] && (i[T] = O, d = !0);
      }
    }
  } else {
    wr(e, t, r, i) && (d = !0);
    let a;
    for (const p in l)
      (!t || // for camelCase
      !L(t, p) && // it's possible the original props was passed in as kebab-case
      // and converted to camelCase (#955)
      ((a = Ze(p)) === p || !L(t, a))) && (f ? s && // for camelCase
      (s[p] !== void 0 || // for kebab-case
      s[a] !== void 0) && (r[p] = As(
        f,
        l,
        p,
        void 0,
        e,
        !0
      )) : delete r[p]);
    if (i !== l)
      for (const p in i)
        (!t || !L(t, p)) && (delete i[p], d = !0);
  }
  d && Fe(e.attrs, "set", "");
}
function wr(e, t, s, n) {
  const [r, i] = e.propsOptions;
  let o = !1, l;
  if (t)
    for (let f in t) {
      if (mt(f))
        continue;
      const d = t[f];
      let a;
      r && L(r, a = he(f)) ? !i || !i.includes(a) ? s[a] = d : (l || (l = {}))[a] = d : is(e.emitsOptions, f) || (!(f in n) || d !== n[f]) && (n[f] = d, o = !0);
    }
  if (i) {
    const f = /* @__PURE__ */ N(s), d = l || k;
    for (let a = 0; a < i.length; a++) {
      const p = i[a];
      s[p] = As(
        r,
        f,
        p,
        d[p],
        e,
        !L(d, p)
      );
    }
  }
  return o;
}
function As(e, t, s, n, r, i) {
  const o = e[s];
  if (o != null) {
    const l = L(o, "default");
    if (l && n === void 0) {
      const f = o.default;
      if (o.type !== Function && !o.skipFactory && j(f)) {
        const { propsDefaults: d } = r;
        if (s in d)
          n = d[s];
        else {
          const a = Mt(r);
          n = d[s] = f.call(
            null,
            t
          ), a();
        }
      } else
        n = f;
      r.ce && r.ce._setProp(s, n);
    }
    o[
      0
      /* shouldCast */
    ] && (i && !l ? n = !1 : o[
      1
      /* shouldCastTrue */
    ] && (n === "" || n === Ze(s)) && (n = !0));
  }
  return n;
}
const bo = /* @__PURE__ */ new WeakMap();
function Cr(e, t, s = !1) {
  const n = s ? bo : t.propsCache, r = n.get(e);
  if (r)
    return r;
  const i = e.props, o = {}, l = [];
  let f = !1;
  if (!j(e)) {
    const a = (p) => {
      f = !0;
      const [T, O] = Cr(p, t, !0);
      Z(o, T), O && l.push(...O);
    };
    !s && t.mixins.length && t.mixins.forEach(a), e.extends && a(e.extends), e.mixins && e.mixins.forEach(a);
  }
  if (!i && !f)
    return U(e) && n.set(e, tt), tt;
  if (R(i))
    for (let a = 0; a < i.length; a++) {
      const p = he(i[a]);
      _n(p) && (o[p] = k);
    }
  else if (i)
    for (const a in i) {
      const p = he(a);
      if (_n(p)) {
        const T = i[a], O = o[p] = R(T) || j(T) ? { type: T } : Z({}, T), H = O.type;
        let v = !1, A = !0;
        if (R(H))
          for (let F = 0; F < H.length; ++F) {
            const C = H[F], D = j(C) && C.name;
            if (D === "Boolean") {
              v = !0;
              break;
            } else D === "String" && (A = !1);
          }
        else
          v = j(H) && H.name === "Boolean";
        O[
          0
          /* shouldCast */
        ] = v, O[
          1
          /* shouldCastTrue */
        ] = A, (v || L(O, "default")) && l.push(p);
      }
    }
  const d = [o, l];
  return U(e) && n.set(e, d), d;
}
function _n(e) {
  return e[0] !== "$" && !mt(e);
}
const ks = (e) => e === "_" || e === "_ctx" || e === "$stable", qs = (e) => R(e) ? e.map(Ce) : [Ce(e)], yo = (e, t, s) => {
  if (t._n)
    return t;
  const n = Ri((...r) => qs(t(...r)), s);
  return n._c = !1, n;
}, Tr = (e, t, s) => {
  const n = e._ctx;
  for (const r in e) {
    if (ks(r)) continue;
    const i = e[r];
    if (j(i))
      t[r] = yo(r, i, n);
    else if (i != null) {
      const o = qs(i);
      t[r] = () => o;
    }
  }
}, Or = (e, t) => {
  const s = qs(t);
  e.slots.default = () => s;
}, Er = (e, t, s) => {
  for (const n in t)
    (s || !ks(n)) && (e[n] = t[n]);
}, vo = (e, t, s) => {
  const n = e.slots = xr();
  if (e.vnode.shapeFlag & 32) {
    const r = t._;
    r ? (Er(n, t, s), s && Ln(n, "_", r, !0)) : Tr(t, n);
  } else t && Or(e, t);
}, xo = (e, t, s) => {
  const { vnode: n, slots: r } = e;
  let i = !0, o = k;
  if (n.shapeFlag & 32) {
    const l = t._;
    l ? s && l === 1 ? i = !1 : Er(r, t, s) : (i = !t.$stable, Tr(t, r)), o = t;
  } else t && (Or(e, t), o = { default: 1 });
  if (i)
    for (const l in r)
      !ks(l) && o[l] == null && delete r[l];
}, ie = Oo;
function So(e) {
  return wo(e);
}
function wo(e, t) {
  const s = es();
  s.__VUE__ = !0;
  const {
    insert: n,
    remove: r,
    patchProp: i,
    createElement: o,
    createText: l,
    createComment: f,
    setText: d,
    setElementText: a,
    parentNode: p,
    nextSibling: T,
    setScopeId: O = Ee,
    insertStaticContent: H
  } = e, v = (c, u, h, b = null, g = null, _ = null, S = void 0, x = null, y = !!u.dynamicChildren) => {
    if (c === u)
      return;
    c && !pt(c, u) && (b = $t(c), me(c, g, _, !0), c = null), u.patchFlag === -2 && (y = !1, u.dynamicChildren = null);
    const { type: m, ref: I, shapeFlag: w } = u;
    switch (m) {
      case os:
        A(c, u, h, b);
        break;
      case ot:
        F(c, u, h, b);
        break;
      case ms:
        c == null && C(u, h, b, S);
        break;
      case le:
        Ft(
          c,
          u,
          h,
          b,
          g,
          _,
          S,
          x,
          y
        );
        break;
      default:
        w & 1 ? z(
          c,
          u,
          h,
          b,
          g,
          _,
          S,
          x,
          y
        ) : w & 6 ? jt(
          c,
          u,
          h,
          b,
          g,
          _,
          S,
          x,
          y
        ) : (w & 64 || w & 128) && m.process(
          c,
          u,
          h,
          b,
          g,
          _,
          S,
          x,
          y,
          ut
        );
    }
    I != null && g ? vt(I, c && c.ref, _, u || c, !u) : I == null && c && c.ref != null && vt(c.ref, null, _, c, !0);
  }, A = (c, u, h, b) => {
    if (c == null)
      n(
        u.el = l(u.children),
        h,
        b
      );
    else {
      const g = u.el = c.el;
      u.children !== c.children && d(g, u.children);
    }
  }, F = (c, u, h, b) => {
    c == null ? n(
      u.el = f(u.children || ""),
      h,
      b
    ) : u.el = c.el;
  }, C = (c, u, h, b) => {
    [c.el, c.anchor] = H(
      c.children,
      u,
      h,
      b,
      c.el,
      c.anchor
    );
  }, D = ({ el: c, anchor: u }, h, b) => {
    let g;
    for (; c && c !== u; )
      g = T(c), n(c, h, b), c = g;
    n(u, h, b);
  }, M = ({ el: c, anchor: u }) => {
    let h;
    for (; c && c !== u; )
      h = T(c), r(c), c = h;
    r(u);
  }, z = (c, u, h, b, g, _, S, x, y) => {
    if (u.type === "svg" ? S = "svg" : u.type === "math" && (S = "mathml"), c == null)
      Ne(
        u,
        h,
        b,
        g,
        _,
        S,
        x,
        y
      );
    else {
      const m = c.el && c.el._isVueCE ? c.el : null;
      try {
        m && m._beginPatch(), Rt(
          c,
          u,
          g,
          _,
          S,
          x,
          y
        );
      } finally {
        m && m._endPatch();
      }
    }
  }, Ne = (c, u, h, b, g, _, S, x) => {
    let y, m;
    const { props: I, shapeFlag: w, transition: E, dirs: P } = c;
    if (y = c.el = o(
      c.type,
      _,
      I && I.is,
      I
    ), w & 8 ? a(y, c.children) : w & 16 && Le(
      c.children,
      y,
      null,
      b,
      g,
      _s(c, _),
      S,
      x
    ), P && qe(c, null, b, "created"), _e(y, c, c.scopeId, S, b), I) {
      for (const V in I)
        V !== "value" && !mt(V) && i(y, V, null, I[V], _, b);
      "value" in I && i(y, "value", null, I.value, _), (m = I.onVnodeBeforeMount) && xe(m, b, c);
    }
    P && qe(c, null, b, "beforeMount");
    const $ = Co(g, E);
    $ && E.beforeEnter(y), n(y, u, h), ((m = I && I.onVnodeMounted) || $ || P) && ie(() => {
      try {
        m && xe(m, b, c), $ && E.enter(y), P && qe(c, null, b, "mounted");
      } finally {
      }
    }, g);
  }, _e = (c, u, h, b, g) => {
    if (h && O(c, h), b)
      for (let _ = 0; _ < b.length; _++)
        O(c, b[_]);
    if (g) {
      let _ = g.subTree;
      if (u === _ || Mr(_.type) && (_.ssContent === u || _.ssFallback === u)) {
        const S = g.vnode;
        _e(
          c,
          S,
          S.scopeId,
          S.slotScopeIds,
          g.parent
        );
      }
    }
  }, Le = (c, u, h, b, g, _, S, x, y = 0) => {
    for (let m = y; m < c.length; m++) {
      const I = c[m] = x ? Re(c[m]) : Ce(c[m]);
      v(
        null,
        I,
        u,
        h,
        b,
        g,
        _,
        S,
        x
      );
    }
  }, Rt = (c, u, h, b, g, _, S) => {
    const x = u.el = c.el;
    let { patchFlag: y, dynamicChildren: m, dirs: I } = u;
    y |= c.patchFlag & 16;
    const w = c.props || k, E = u.props || k;
    let P;
    if (h && Ge(h, !1), (P = E.onVnodeBeforeUpdate) && xe(P, h, u, c), I && qe(u, c, h, "beforeUpdate"), h && Ge(h, !0), (w.innerHTML && E.innerHTML == null || w.textContent && E.textContent == null) && a(x, ""), m ? We(
      c.dynamicChildren,
      m,
      x,
      h,
      b,
      _s(u, g),
      _
    ) : S || K(
      c,
      u,
      x,
      null,
      h,
      b,
      _s(u, g),
      _,
      !1
    ), y > 0) {
      if (y & 16)
        ct(x, w, E, h, g);
      else if (y & 2 && w.class !== E.class && i(x, "class", null, E.class, g), y & 4 && i(x, "style", w.style, E.style, g), y & 8) {
        const $ = u.dynamicProps;
        for (let V = 0; V < $.length; V++) {
          const W = $[V], G = w[W], X = E[W];
          (X !== G || W === "value") && i(x, W, G, X, g, h);
        }
      }
      y & 1 && c.children !== u.children && a(x, u.children);
    } else !S && m == null && ct(x, w, E, h, g);
    ((P = E.onVnodeUpdated) || I) && ie(() => {
      P && xe(P, h, u, c), I && qe(u, c, h, "updated");
    }, b);
  }, We = (c, u, h, b, g, _, S) => {
    for (let x = 0; x < u.length; x++) {
      const y = c[x], m = u[x], I = (
        // oldVNode may be an errored async setup() component inside Suspense
        // which will not have a mounted element
        y.el && // - In the case of a Fragment, we need to provide the actual parent
        // of the Fragment itself so it can move its children.
        (y.type === le || // - In the case of different nodes, there is going to be a replacement
        // which also requires the correct parent container
        !pt(y, m) || // - In the case of a component, it could contain anything.
        y.shapeFlag & 198) ? p(y.el) : (
          // In other cases, the parent container is not actually used so we
          // just pass the block element here to avoid a DOM parentNode call.
          h
        )
      );
      v(
        y,
        m,
        I,
        null,
        b,
        g,
        _,
        S,
        !0
      );
    }
  }, ct = (c, u, h, b, g) => {
    if (u !== h) {
      if (u !== k)
        for (const _ in u)
          !mt(_) && !(_ in h) && i(
            c,
            _,
            u[_],
            null,
            g,
            b
          );
      for (const _ in h) {
        if (mt(_)) continue;
        const S = h[_], x = u[_];
        S !== x && _ !== "value" && i(c, _, x, S, g, b);
      }
      "value" in h && i(c, "value", u.value, h.value, g);
    }
  }, Ft = (c, u, h, b, g, _, S, x, y) => {
    const m = u.el = c ? c.el : l(""), I = u.anchor = c ? c.anchor : l("");
    let { patchFlag: w, dynamicChildren: E, slotScopeIds: P } = u;
    P && (x = x ? x.concat(P) : P), c == null ? (n(m, h, b), n(I, h, b), Le(
      // #10007
      // such fragment like `<></>` will be compiled into
      // a fragment which doesn't have a children.
      // In this case fallback to an empty array
      u.children || [],
      h,
      I,
      g,
      _,
      S,
      x,
      y
    )) : w > 0 && w & 64 && E && // #2715 the previous fragment could've been a BAILed one as a result
    // of renderSlot() with no valid children
    c.dynamicChildren && c.dynamicChildren.length === E.length ? (We(
      c.dynamicChildren,
      E,
      h,
      g,
      _,
      S,
      x
    ), // #2080 if the stable fragment has a key, it's a <template v-for> that may
    //  get moved around. Make sure all root level vnodes inherit el.
    // #2134 or if it's a component root, it may also get moved around
    // as the component is being moved.
    (u.key != null || g && u === g.subTree) && Ar(
      c,
      u,
      !0
      /* shallow */
    )) : K(
      c,
      u,
      h,
      I,
      g,
      _,
      S,
      x,
      y
    );
  }, jt = (c, u, h, b, g, _, S, x, y) => {
    u.slotScopeIds = x, c == null ? u.shapeFlag & 512 ? g.ctx.activate(
      u,
      h,
      b,
      S,
      y
    ) : ls(
      u,
      h,
      b,
      g,
      _,
      S,
      y
    ) : Ys(c, u, y);
  }, ls = (c, u, h, b, g, _, S) => {
    const x = c.component = jo(
      c,
      b,
      g
    );
    if (dr(c) && (x.ctx.renderer = ut), $o(x, !1, S), x.asyncDep) {
      if (g && g.registerDep(x, te, S), !c.el) {
        const y = x.subTree = Xe(ot);
        F(null, y, u, h), c.placeholder = y.el;
      }
    } else
      te(
        x,
        c,
        u,
        h,
        g,
        _,
        S
      );
  }, Ys = (c, u, h) => {
    const b = u.component = c.component;
    if (po(c, u, h))
      if (b.asyncDep && !b.asyncResolved) {
        q(b, u, h);
        return;
      } else
        b.next = u, b.update();
    else
      u.el = c.el, b.vnode = u;
  }, te = (c, u, h, b, g, _, S) => {
    const x = () => {
      if (c.isMounted) {
        let { next: w, bu: E, u: P, parent: $, vnode: V } = c;
        {
          const ye = Ir(c);
          if (ye) {
            w && (w.el = V.el, q(c, w, S)), ye.asyncDep.then(() => {
              ie(() => {
                c.isUnmounted || m();
              }, g);
            });
            return;
          }
        }
        let W = w, G;
        Ge(c, !1), w ? (w.el = V.el, q(c, w, S)) : w = V, E && us(E), (G = w.props && w.props.onVnodeBeforeUpdate) && xe(G, $, w, V), Ge(c, !0);
        const X = pn(c), be = c.subTree;
        c.subTree = X, v(
          be,
          X,
          // parent may have changed if it's in a teleport
          p(be.el),
          // anchor may have changed if it's in a fragment
          $t(be),
          c,
          g,
          _
        ), w.el = X.el, W === null && go(c, X.el), P && ie(P, g), (G = w.props && w.props.onVnodeUpdated) && ie(
          () => xe(G, $, w, V),
          g
        );
      } else {
        let w;
        const { el: E, props: P } = u, { bm: $, m: V, parent: W, root: G, type: X } = c, be = xt(u);
        Ge(c, !1), $ && us($), !be && (w = P && P.onVnodeBeforeMount) && xe(w, W, u), Ge(c, !0);
        {
          G.ce && G.ce._hasShadowRoot() && G.ce._injectChildStyle(
            X,
            c.parent ? c.parent.type : void 0
          );
          const ye = c.subTree = pn(c);
          v(
            null,
            ye,
            h,
            b,
            c,
            g,
            _
          ), u.el = ye.el;
        }
        if (V && ie(V, g), !be && (w = P && P.onVnodeMounted)) {
          const ye = u;
          ie(
            () => xe(w, W, ye),
            g
          );
        }
        (u.shapeFlag & 256 || W && xt(W.vnode) && W.vnode.shapeFlag & 256) && c.a && ie(c.a, g), c.isMounted = !0, u = h = b = null;
      }
    };
    c.scope.on();
    const y = c.effect = new Wn(x);
    c.scope.off();
    const m = c.update = y.run.bind(y), I = c.job = y.runIfDirty.bind(y);
    I.i = c, I.id = c.uid, y.scheduler = () => Ws(I), Ge(c, !0), m();
  }, q = (c, u, h) => {
    u.component = c;
    const b = c.vnode.props;
    c.vnode = u, c.next = null, mo(c, u.props, b, h), xo(c, u.children, h), je(), ln(c), De();
  }, K = (c, u, h, b, g, _, S, x, y = !1) => {
    const m = c && c.children, I = c ? c.shapeFlag : 0, w = u.children, { patchFlag: E, shapeFlag: P } = u;
    if (E > 0) {
      if (E & 128) {
        Dt(
          m,
          w,
          h,
          b,
          g,
          _,
          S,
          x,
          y
        );
        return;
      } else if (E & 256) {
        Be(
          m,
          w,
          h,
          b,
          g,
          _,
          S,
          x,
          y
        );
        return;
      }
    }
    P & 8 ? (I & 16 && ft(m, g, _), w !== m && a(h, w)) : I & 16 ? P & 16 ? Dt(
      m,
      w,
      h,
      b,
      g,
      _,
      S,
      x,
      y
    ) : ft(m, g, _, !0) : (I & 8 && a(h, ""), P & 16 && Le(
      w,
      h,
      b,
      g,
      _,
      S,
      x,
      y
    ));
  }, Be = (c, u, h, b, g, _, S, x, y) => {
    c = c || tt, u = u || tt;
    const m = c.length, I = u.length, w = Math.min(m, I);
    let E;
    for (E = 0; E < w; E++) {
      const P = u[E] = y ? Re(u[E]) : Ce(u[E]);
      v(
        c[E],
        P,
        h,
        null,
        g,
        _,
        S,
        x,
        y
      );
    }
    m > I ? ft(
      c,
      g,
      _,
      !0,
      !1,
      w
    ) : Le(
      u,
      h,
      b,
      g,
      _,
      S,
      x,
      y,
      w
    );
  }, Dt = (c, u, h, b, g, _, S, x, y) => {
    let m = 0;
    const I = u.length;
    let w = c.length - 1, E = I - 1;
    for (; m <= w && m <= E; ) {
      const P = c[m], $ = u[m] = y ? Re(u[m]) : Ce(u[m]);
      if (pt(P, $))
        v(
          P,
          $,
          h,
          null,
          g,
          _,
          S,
          x,
          y
        );
      else
        break;
      m++;
    }
    for (; m <= w && m <= E; ) {
      const P = c[w], $ = u[E] = y ? Re(u[E]) : Ce(u[E]);
      if (pt(P, $))
        v(
          P,
          $,
          h,
          null,
          g,
          _,
          S,
          x,
          y
        );
      else
        break;
      w--, E--;
    }
    if (m > w) {
      if (m <= E) {
        const P = E + 1, $ = P < I ? u[P].el : b;
        for (; m <= E; )
          v(
            null,
            u[m] = y ? Re(u[m]) : Ce(u[m]),
            h,
            $,
            g,
            _,
            S,
            x,
            y
          ), m++;
      }
    } else if (m > E)
      for (; m <= w; )
        me(c[m], g, _, !0), m++;
    else {
      const P = m, $ = m, V = /* @__PURE__ */ new Map();
      for (m = $; m <= E; m++) {
        const ce = u[m] = y ? Re(u[m]) : Ce(u[m]);
        ce.key != null && V.set(ce.key, m);
      }
      let W, G = 0;
      const X = E - $ + 1;
      let be = !1, ye = 0;
      const at = new Array(X);
      for (m = 0; m < X; m++) at[m] = 0;
      for (m = P; m <= w; m++) {
        const ce = c[m];
        if (G >= X) {
          me(ce, g, _, !0);
          continue;
        }
        let ve;
        if (ce.key != null)
          ve = V.get(ce.key);
        else
          for (W = $; W <= E; W++)
            if (at[W - $] === 0 && pt(ce, u[W])) {
              ve = W;
              break;
            }
        ve === void 0 ? me(ce, g, _, !0) : (at[ve - $] = m + 1, ve >= ye ? ye = ve : be = !0, v(
          ce,
          u[ve],
          h,
          null,
          g,
          _,
          S,
          x,
          y
        ), G++);
      }
      const Zs = be ? To(at) : tt;
      for (W = Zs.length - 1, m = X - 1; m >= 0; m--) {
        const ce = $ + m, ve = u[ce], Qs = u[ce + 1], en = ce + 1 < I ? (
          // #13559, #14173 fallback to el placeholder for unresolved async component
          Qs.el || Pr(Qs)
        ) : b;
        at[m] === 0 ? v(
          null,
          ve,
          h,
          en,
          g,
          _,
          S,
          x,
          y
        ) : be && (W < 0 || m !== Zs[W] ? ke(ve, h, en, 2) : W--);
      }
    }
  }, ke = (c, u, h, b, g = null) => {
    const { el: _, type: S, transition: x, children: y, shapeFlag: m } = c;
    if (m & 6) {
      ke(c.component.subTree, u, h, b);
      return;
    }
    if (m & 128) {
      c.suspense.move(u, h, b);
      return;
    }
    if (m & 64) {
      S.move(c, u, h, ut);
      return;
    }
    if (S === le) {
      n(_, u, h);
      for (let w = 0; w < y.length; w++)
        ke(y[w], u, h, b);
      n(c.anchor, u, h);
      return;
    }
    if (S === ms) {
      D(c, u, h);
      return;
    }
    if (b !== 2 && m & 1 && x)
      if (b === 0)
        x.beforeEnter(_), n(_, u, h), ie(() => x.enter(_), g);
      else {
        const { leave: w, delayLeave: E, afterLeave: P } = x, $ = () => {
          c.ctx.isUnmounted ? r(_) : n(_, u, h);
        }, V = () => {
          _._isLeaving && _[Li](
            !0
            /* cancelled */
          ), w(_, () => {
            $(), P && P();
          });
        };
        E ? E(_, $, V) : V();
      }
    else
      n(_, u, h);
  }, me = (c, u, h, b = !1, g = !1) => {
    const {
      type: _,
      props: S,
      ref: x,
      children: y,
      dynamicChildren: m,
      shapeFlag: I,
      patchFlag: w,
      dirs: E,
      cacheIndex: P,
      memo: $
    } = c;
    if (w === -2 && (g = !1), x != null && (je(), vt(x, null, h, c, !0), De()), P != null && (u.renderCache[P] = void 0), I & 256) {
      u.ctx.deactivate(c);
      return;
    }
    const V = I & 1 && E, W = !xt(c);
    let G;
    if (W && (G = S && S.onVnodeBeforeUnmount) && xe(G, u, c), I & 6)
      Lr(c.component, h, b);
    else {
      if (I & 128) {
        c.suspense.unmount(h, b);
        return;
      }
      V && qe(c, null, u, "beforeUnmount"), I & 64 ? c.type.remove(
        c,
        u,
        h,
        ut,
        b
      ) : m && // #5154
      // when v-once is used inside a block, setBlockTracking(-1) marks the
      // parent block with hasOnce: true
      // so that it doesn't take the fast path during unmount - otherwise
      // components nested in v-once are never unmounted.
      !m.hasOnce && // #1153: fast path should not be taken for non-stable (v-for) fragments
      (_ !== le || w > 0 && w & 64) ? ft(
        m,
        u,
        h,
        !1,
        !0
      ) : (_ === le && w & 384 || !g && I & 16) && ft(y, u, h), b && zs(c);
    }
    const X = $ != null && P == null;
    (W && (G = S && S.onVnodeUnmounted) || V || X) && ie(() => {
      G && xe(G, u, c), V && qe(c, null, u, "unmounted"), X && (c.el = null);
    }, h);
  }, zs = (c) => {
    const { type: u, el: h, anchor: b, transition: g } = c;
    if (u === le) {
      Nr(h, b);
      return;
    }
    if (u === ms) {
      M(c);
      return;
    }
    const _ = () => {
      r(h), g && !g.persisted && g.afterLeave && g.afterLeave();
    };
    if (c.shapeFlag & 1 && g && !g.persisted) {
      const { leave: S, delayLeave: x } = g, y = () => S(h, _);
      x ? x(c.el, _, y) : y();
    } else
      _();
  }, Nr = (c, u) => {
    let h;
    for (; c !== u; )
      h = T(c), r(c), c = h;
    r(u);
  }, Lr = (c, u, h) => {
    const { bum: b, scope: g, job: _, subTree: S, um: x, m: y, a: m } = c;
    mn(y), mn(m), b && us(b), g.stop(), _ && (_.flags |= 8, me(S, c, u, h)), x && ie(x, u), ie(() => {
      c.isUnmounted = !0;
    }, u);
  }, ft = (c, u, h, b = !1, g = !1, _ = 0) => {
    for (let S = _; S < c.length; S++)
      me(c[S], u, h, b, g);
  }, $t = (c) => {
    if (c.shapeFlag & 6)
      return $t(c.component.subTree);
    if (c.shapeFlag & 128)
      return c.suspense.next();
    const u = T(c.anchor || c.el), h = u && u[Hi];
    return h ? T(h) : u;
  };
  let cs = !1;
  const Xs = (c, u, h) => {
    let b;
    c == null ? u._vnode && (me(u._vnode, null, null, !0), b = u._vnode.component) : v(
      u._vnode || null,
      c,
      u,
      null,
      null,
      null,
      h
    ), u._vnode = c, cs || (cs = !0, ln(b), or(), cs = !1);
  }, ut = {
    p: v,
    um: me,
    m: ke,
    r: zs,
    mt: ls,
    mc: Le,
    pc: K,
    pbc: We,
    n: $t,
    o: e
  };
  return {
    render: Xs,
    hydrate: void 0,
    createApp: lo(Xs)
  };
}
function _s({ type: e, props: t }, s) {
  return s === "svg" && e === "foreignObject" || s === "mathml" && e === "annotation-xml" && t && t.encoding && t.encoding.includes("html") ? void 0 : s;
}
function Ge({ effect: e, job: t }, s) {
  s ? (e.flags |= 32, t.flags |= 4) : (e.flags &= -33, t.flags &= -5);
}
function Co(e, t) {
  return (!e || e && !e.pendingBranch) && t && !t.persisted;
}
function Ar(e, t, s = !1) {
  const n = e.children, r = t.children;
  if (R(n) && R(r))
    for (let i = 0; i < n.length; i++) {
      const o = n[i];
      let l = r[i];
      l.shapeFlag & 1 && !l.dynamicChildren && ((l.patchFlag <= 0 || l.patchFlag === 32) && (l = r[i] = Re(r[i]), l.el = o.el), !s && l.patchFlag !== -2 && Ar(o, l)), l.type === os && (l.patchFlag === -1 && (l = r[i] = Re(l)), l.el = o.el), l.type === ot && !l.el && (l.el = o.el);
    }
}
function To(e) {
  const t = e.slice(), s = [0];
  let n, r, i, o, l;
  const f = e.length;
  for (n = 0; n < f; n++) {
    const d = e[n];
    if (d !== 0) {
      if (r = s[s.length - 1], e[r] < d) {
        t[n] = r, s.push(n);
        continue;
      }
      for (i = 0, o = s.length - 1; i < o; )
        l = i + o >> 1, e[s[l]] < d ? i = l + 1 : o = l;
      d < e[s[i]] && (i > 0 && (t[n] = s[i - 1]), s[i] = n);
    }
  }
  for (i = s.length, o = s[i - 1]; i-- > 0; )
    s[i] = o, o = t[o];
  return s;
}
function Ir(e) {
  const t = e.subTree.component;
  if (t)
    return t.asyncDep && !t.asyncResolved ? t : Ir(t);
}
function mn(e) {
  if (e)
    for (let t = 0; t < e.length; t++)
      e[t].flags |= 8;
}
function Pr(e) {
  if (e.placeholder)
    return e.placeholder;
  const t = e.component;
  return t ? Pr(t.subTree) : null;
}
const Mr = (e) => e.__isSuspense;
function Oo(e, t) {
  t && t.pendingBranch ? R(e) ? t.effects.push(...e) : t.effects.push(e) : Mi(e);
}
const le = /* @__PURE__ */ Symbol.for("v-fgt"), os = /* @__PURE__ */ Symbol.for("v-txt"), ot = /* @__PURE__ */ Symbol.for("v-cmt"), ms = /* @__PURE__ */ Symbol.for("v-stc"), wt = [];
let fe = null;
function ae(e = !1) {
  wt.push(fe = e ? null : []);
}
function Eo() {
  wt.pop(), fe = wt[wt.length - 1] || null;
}
let Et = 1;
function bn(e, t = !1) {
  Et += e, e < 0 && fe && t && (fe.hasOnce = !0);
}
function Ao(e) {
  return e.dynamicChildren = Et > 0 ? fe || tt : null, Eo(), Et > 0 && fe && fe.push(e), e;
}
function de(e, t, s, n, r, i) {
  return Ao(
    J(
      e,
      t,
      s,
      n,
      r,
      i,
      !0
    )
  );
}
function Rr(e) {
  return e ? e.__v_isVNode === !0 : !1;
}
function pt(e, t) {
  return e.type === t.type && e.key === t.key;
}
const Fr = ({ key: e }) => e ?? null, Wt = ({
  ref: e,
  ref_key: t,
  ref_for: s
}) => (typeof e == "number" && (e = "" + e), e != null ? Y(e) || /* @__PURE__ */ ee(e) || j(e) ? { i: Oe, r: e, k: t, f: !!s } : e : null);
function J(e, t = null, s = null, n = 0, r = null, i = e === le ? 0 : 1, o = !1, l = !1) {
  const f = {
    __v_isVNode: !0,
    __v_skip: !0,
    type: e,
    props: t,
    key: t && Fr(t),
    ref: t && Wt(t),
    scopeId: cr,
    slotScopeIds: null,
    children: s,
    component: null,
    suspense: null,
    ssContent: null,
    ssFallback: null,
    dirs: null,
    transition: null,
    el: null,
    anchor: null,
    target: null,
    targetStart: null,
    targetAnchor: null,
    staticCount: 0,
    shapeFlag: i,
    patchFlag: n,
    dynamicProps: r,
    dynamicChildren: null,
    appContext: null,
    ctx: Oe
  };
  return l ? (Gs(f, s), i & 128 && e.normalize(f)) : s && (f.shapeFlag |= Y(s) ? 8 : 16), Et > 0 && // avoid a block node from tracking itself
  !o && // has current parent block
  fe && // presence of a patch flag indicates this node needs patching on updates.
  // component nodes also should always be patched, because even if the
  // component doesn't need to update, it needs to persist the instance on to
  // the next vnode so that it can be properly unmounted later.
  (f.patchFlag > 0 || i & 6) && // the EVENTS flag is only for hydration and if it is the only flag, the
  // vnode should not be considered dynamic due to handler caching.
  f.patchFlag !== 32 && fe.push(f), f;
}
const Xe = Io;
function Io(e, t = null, s = null, n = 0, r = null, i = !1) {
  if ((!e || e === Qi) && (e = ot), Rr(e)) {
    const l = lt(
      e,
      t,
      !0
      /* mergeRef: true */
    );
    return s && Gs(l, s), Et > 0 && !i && fe && (l.shapeFlag & 6 ? fe[fe.indexOf(e)] = l : fe.push(l)), l.patchFlag = -2, l;
  }
  if (Uo(e) && (e = e.__vccOpts), t) {
    t = Po(t);
    let { class: l, style: f } = t;
    l && !Y(l) && (t.class = js(l)), U(f) && (/* @__PURE__ */ Ks(f) && !R(f) && (f = Z({}, f)), t.style = Ve(f));
  }
  const o = Y(e) ? 1 : Mr(e) ? 128 : Ni(e) ? 64 : U(e) ? 4 : j(e) ? 2 : 0;
  return J(
    e,
    t,
    s,
    n,
    r,
    o,
    i,
    !0
  );
}
function Po(e) {
  return e ? /* @__PURE__ */ Ks(e) || Sr(e) ? Z({}, e) : e : null;
}
function lt(e, t, s = !1, n = !1) {
  const { props: r, ref: i, patchFlag: o, children: l, transition: f } = e, d = t ? Mo(r || {}, t) : r, a = {
    __v_isVNode: !0,
    __v_skip: !0,
    type: e.type,
    props: d,
    key: d && Fr(d),
    ref: t && t.ref ? (
      // #2078 in the case of <component :is="vnode" ref="extra"/>
      // if the vnode itself already has a ref, cloneVNode will need to merge
      // the refs so the single vnode can be set on multiple refs
      s && i ? R(i) ? i.concat(Wt(t)) : [i, Wt(t)] : Wt(t)
    ) : i,
    scopeId: e.scopeId,
    slotScopeIds: e.slotScopeIds,
    children: l,
    target: e.target,
    targetStart: e.targetStart,
    targetAnchor: e.targetAnchor,
    staticCount: e.staticCount,
    shapeFlag: e.shapeFlag,
    // if the vnode is cloned with extra props, we can no longer assume its
    // existing patch flag to be reliable and need to add the FULL_PROPS flag.
    // note: preserve flag for fragments since they use the flag for children
    // fast paths only.
    patchFlag: t && e.type !== le ? o === -1 ? 16 : o | 16 : o,
    dynamicProps: e.dynamicProps,
    dynamicChildren: e.dynamicChildren,
    appContext: e.appContext,
    dirs: e.dirs,
    transition: f,
    // These should technically only be non-null on mounted VNodes. However,
    // they *should* be copied for kept-alive vnodes. So we just always copy
    // them since them being non-null during a mount doesn't affect the logic as
    // they will simply be overwritten.
    component: e.component,
    suspense: e.suspense,
    ssContent: e.ssContent && lt(e.ssContent),
    ssFallback: e.ssFallback && lt(e.ssFallback),
    placeholder: e.placeholder,
    el: e.el,
    anchor: e.anchor,
    ctx: e.ctx,
    ce: e.ce
  };
  return f && n && Bs(
    a,
    f.clone(a)
  ), a;
}
function jr(e = " ", t = 0) {
  return Xe(os, null, e, t);
}
function Ce(e) {
  return e == null || typeof e == "boolean" ? Xe(ot) : R(e) ? Xe(
    le,
    null,
    // #3666, avoid reference pollution when reusing vnode
    e.slice()
  ) : Rr(e) ? Re(e) : Xe(os, null, String(e));
}
function Re(e) {
  return e.el === null && e.patchFlag !== -1 || e.memo ? e : lt(e);
}
function Gs(e, t) {
  let s = 0;
  const { shapeFlag: n } = e;
  if (t == null)
    t = null;
  else if (R(t))
    s = 16;
  else if (typeof t == "object")
    if (n & 65) {
      const r = t.default;
      r && (r._c && (r._d = !1), Gs(e, r()), r._c && (r._d = !0));
      return;
    } else {
      s = 32;
      const r = t._;
      !r && !Sr(t) ? t._ctx = Oe : r === 3 && Oe && (Oe.slots._ === 1 ? t._ = 1 : (t._ = 2, e.patchFlag |= 1024));
    }
  else j(t) ? (t = { default: t, _ctx: Oe }, s = 32) : (t = String(t), n & 64 ? (s = 16, t = [jr(t)]) : s = 8);
  e.children = t, e.shapeFlag |= s;
}
function Mo(...e) {
  const t = {};
  for (let s = 0; s < e.length; s++) {
    const n = e[s];
    for (const r in n)
      if (r === "class")
        t.class !== n.class && (t.class = js([t.class, n.class]));
      else if (r === "style")
        t.style = Ve([t.style, n.style]);
      else if (Xt(r)) {
        const i = t[r], o = n[r];
        o && i !== o && !(R(i) && i.includes(o)) ? t[r] = i ? [].concat(i, o) : o : o == null && i == null && // mergeProps({ 'onUpdate:modelValue': undefined }) should not retain
        // the model listener.
        !Zt(r) && (t[r] = o);
      } else r !== "" && (t[r] = n[r]);
  }
  return t;
}
function xe(e, t, s, n = null) {
  Ie(e, t, 7, [
    s,
    n
  ]);
}
const Ro = mr();
let Fo = 0;
function jo(e, t, s) {
  const n = e.type, r = (t ? t.appContext : e.appContext) || Ro, i = {
    uid: Fo++,
    vnode: e,
    type: n,
    parent: t,
    appContext: r,
    root: null,
    // to be immediately set
    next: null,
    subTree: null,
    // will be set synchronously right after creation
    effect: null,
    update: null,
    // will be set synchronously right after creation
    job: null,
    scope: new Zr(
      !0
      /* detached */
    ),
    render: null,
    proxy: null,
    exposed: null,
    exposeProxy: null,
    withProxy: null,
    provides: t ? t.provides : Object.create(r.provides),
    ids: t ? t.ids : ["", 0, 0],
    accessCache: null,
    renderCache: [],
    // local resolved assets
    components: null,
    directives: null,
    // resolved props and emits options
    propsOptions: Cr(n, r),
    emitsOptions: br(n, r),
    // emit
    emit: null,
    // to be set immediately
    emitted: null,
    // props default value
    propsDefaults: k,
    // inheritAttrs
    inheritAttrs: n.inheritAttrs,
    // state
    ctx: k,
    data: k,
    props: k,
    attrs: k,
    slots: k,
    refs: k,
    setupState: k,
    setupContext: null,
    // suspense related
    suspense: s,
    suspenseId: s ? s.pendingId : 0,
    asyncDep: null,
    asyncResolved: !1,
    // lifecycle hooks
    // not using enums here because it results in computed properties
    isMounted: !1,
    isUnmounted: !1,
    isDeactivated: !1,
    bc: null,
    c: null,
    bm: null,
    m: null,
    bu: null,
    u: null,
    um: null,
    bum: null,
    da: null,
    a: null,
    rtg: null,
    rtc: null,
    ec: null,
    sp: null
  };
  return i.ctx = { _: i }, i.root = t ? t.root : i, i.emit = fo.bind(null, i), e.ce && e.ce(i), i;
}
let re = null;
const Do = () => re || Oe;
let zt, Is;
{
  const e = es(), t = (s, n) => {
    let r;
    return (r = e[s]) || (r = e[s] = []), r.push(n), (i) => {
      r.length > 1 ? r.forEach((o) => o(i)) : r[0](i);
    };
  };
  zt = t(
    "__VUE_INSTANCE_SETTERS__",
    (s) => re = s
  ), Is = t(
    "__VUE_SSR_SETTERS__",
    (s) => At = s
  );
}
const Mt = (e) => {
  const t = re;
  return zt(e), e.scope.on(), () => {
    e.scope.off(), zt(t);
  };
}, yn = () => {
  re && re.scope.off(), zt(null);
};
function Dr(e) {
  return e.vnode.shapeFlag & 4;
}
let At = !1;
function $o(e, t = !1, s = !1) {
  t && Is(t);
  const { props: n, children: r } = e.vnode, i = Dr(e);
  _o(e, n, i, t), vo(e, r, s || t);
  const o = i ? Ho(e, t) : void 0;
  return t && Is(!1), o;
}
function Ho(e, t) {
  const s = e.type;
  e.accessCache = /* @__PURE__ */ Object.create(null), e.proxy = new Proxy(e.ctx, eo);
  const { setup: n } = s;
  if (n) {
    je();
    const r = e.setupContext = n.length > 1 ? Lo(e) : null, i = Mt(e), o = Pt(
      n,
      e,
      0,
      [
        e.props,
        r
      ]
    ), l = Dn(o);
    if (De(), i(), (l || e.sp) && !xt(e) && ar(e), l) {
      if (o.then(yn, yn), t)
        return o.then((f) => {
          vn(e, f);
        }).catch((f) => {
          ns(f, e, 0);
        });
      e.asyncDep = o;
    } else
      vn(e, o);
  } else
    $r(e);
}
function vn(e, t, s) {
  j(t) ? e.type.__ssrInlineRender ? e.ssrRender = t : e.render = t : U(t) && (e.setupState = nr(t)), $r(e);
}
function $r(e, t, s) {
  const n = e.type;
  e.render || (e.render = n.render || Ee);
  {
    const r = Mt(e);
    je();
    try {
      to(e);
    } finally {
      De(), r();
    }
  }
}
const No = {
  get(e, t) {
    return Q(e, "get", ""), e[t];
  }
};
function Lo(e) {
  const t = (s) => {
    e.exposed = s || {};
  };
  return {
    attrs: new Proxy(e.attrs, No),
    slots: e.slots,
    emit: e.emit,
    expose: t
  };
}
function Js(e) {
  return e.exposed ? e.exposeProxy || (e.exposeProxy = new Proxy(nr(yi(e.exposed)), {
    get(t, s) {
      if (s in t)
        return t[s];
      if (s in St)
        return St[s](e);
    },
    has(t, s) {
      return s in t || s in St;
    }
  })) : e.proxy;
}
function Uo(e) {
  return j(e) && "__vccOpts" in e;
}
const _t = (e, t) => /* @__PURE__ */ Ti(e, t, At), Vo = "3.5.32";
/**
* @vue/runtime-dom v3.5.32
* (c) 2018-present Yuxi (Evan) You and Vue contributors
* @license MIT
**/
let Ps;
const xn = typeof window < "u" && window.trustedTypes;
if (xn)
  try {
    Ps = /* @__PURE__ */ xn.createPolicy("vue", {
      createHTML: (e) => e
    });
  } catch {
  }
const Hr = Ps ? (e) => Ps.createHTML(e) : (e) => e, Ko = "http://www.w3.org/2000/svg", Wo = "http://www.w3.org/1998/Math/MathML", Me = typeof document < "u" ? document : null, Sn = Me && /* @__PURE__ */ Me.createElement("template"), Bo = {
  insert: (e, t, s) => {
    t.insertBefore(e, s || null);
  },
  remove: (e) => {
    const t = e.parentNode;
    t && t.removeChild(e);
  },
  createElement: (e, t, s, n) => {
    const r = t === "svg" ? Me.createElementNS(Ko, e) : t === "mathml" ? Me.createElementNS(Wo, e) : s ? Me.createElement(e, { is: s }) : Me.createElement(e);
    return e === "select" && n && n.multiple != null && r.setAttribute("multiple", n.multiple), r;
  },
  createText: (e) => Me.createTextNode(e),
  createComment: (e) => Me.createComment(e),
  setText: (e, t) => {
    e.nodeValue = t;
  },
  setElementText: (e, t) => {
    e.textContent = t;
  },
  parentNode: (e) => e.parentNode,
  nextSibling: (e) => e.nextSibling,
  querySelector: (e) => Me.querySelector(e),
  setScopeId(e, t) {
    e.setAttribute(t, "");
  },
  // __UNSAFE__
  // Reason: innerHTML.
  // Static content here can only come from compiled templates.
  // As long as the user only uses trusted templates, this is safe.
  insertStaticContent(e, t, s, n, r, i) {
    const o = s ? s.previousSibling : t.lastChild;
    if (r && (r === i || r.nextSibling))
      for (; t.insertBefore(r.cloneNode(!0), s), !(r === i || !(r = r.nextSibling)); )
        ;
    else {
      Sn.innerHTML = Hr(
        n === "svg" ? `<svg>${e}</svg>` : n === "mathml" ? `<math>${e}</math>` : e
      );
      const l = Sn.content;
      if (n === "svg" || n === "mathml") {
        const f = l.firstChild;
        for (; f.firstChild; )
          l.appendChild(f.firstChild);
        l.removeChild(f);
      }
      t.insertBefore(l, s);
    }
    return [
      // first
      o ? o.nextSibling : t.firstChild,
      // last
      s ? s.previousSibling : t.lastChild
    ];
  }
}, ko = /* @__PURE__ */ Symbol("_vtc");
function qo(e, t, s) {
  const n = e[ko];
  n && (t = (t ? [t, ...n] : [...n]).join(" ")), t == null ? e.removeAttribute("class") : s ? e.setAttribute("class", t) : e.className = t;
}
const wn = /* @__PURE__ */ Symbol("_vod"), Go = /* @__PURE__ */ Symbol("_vsh"), Jo = /* @__PURE__ */ Symbol(""), Yo = /(?:^|;)\s*display\s*:/;
function zo(e, t, s) {
  const n = e.style, r = Y(s);
  let i = !1;
  if (s && !r) {
    if (t)
      if (Y(t))
        for (const o of t.split(";")) {
          const l = o.slice(0, o.indexOf(":")).trim();
          s[l] == null && Bt(n, l, "");
        }
      else
        for (const o in t)
          s[o] == null && Bt(n, o, "");
    for (const o in s)
      o === "display" && (i = !0), Bt(n, o, s[o]);
  } else if (r) {
    if (t !== s) {
      const o = n[Jo];
      o && (s += ";" + o), n.cssText = s, i = Yo.test(s);
    }
  } else t && e.removeAttribute("style");
  wn in e && (e[wn] = i ? n.display : "", e[Go] && (n.display = "none"));
}
const Cn = /\s*!important$/;
function Bt(e, t, s) {
  if (R(s))
    s.forEach((n) => Bt(e, t, n));
  else if (s == null && (s = ""), t.startsWith("--"))
    e.setProperty(t, s);
  else {
    const n = Xo(e, t);
    Cn.test(s) ? e.setProperty(
      Ze(n),
      s.replace(Cn, ""),
      "important"
    ) : e[n] = s;
  }
}
const Tn = ["Webkit", "Moz", "ms"], bs = {};
function Xo(e, t) {
  const s = bs[t];
  if (s)
    return s;
  let n = he(t);
  if (n !== "filter" && n in e)
    return bs[t] = n;
  n = Nn(n);
  for (let r = 0; r < Tn.length; r++) {
    const i = Tn[r] + n;
    if (i in e)
      return bs[t] = i;
  }
  return t;
}
const On = "http://www.w3.org/1999/xlink";
function En(e, t, s, n, r, i = zr(t)) {
  n && t.startsWith("xlink:") ? s == null ? e.removeAttributeNS(On, t.slice(6, t.length)) : e.setAttributeNS(On, t, s) : s == null || i && !Un(s) ? e.removeAttribute(t) : e.setAttribute(
    t,
    i ? "" : Ae(s) ? String(s) : s
  );
}
function An(e, t, s, n, r) {
  if (t === "innerHTML" || t === "textContent") {
    s != null && (e[t] = t === "innerHTML" ? Hr(s) : s);
    return;
  }
  const i = e.tagName;
  if (t === "value" && i !== "PROGRESS" && // custom elements may use _value internally
  !i.includes("-")) {
    const l = i === "OPTION" ? e.getAttribute("value") || "" : e.value, f = s == null ? (
      // #11647: value should be set as empty string for null and undefined,
      // but <input type="checkbox"> should be set as 'on'.
      e.type === "checkbox" ? "on" : ""
    ) : String(s);
    (l !== f || !("_value" in e)) && (e.value = f), s == null && e.removeAttribute(t), e._value = s;
    return;
  }
  let o = !1;
  if (s === "" || s == null) {
    const l = typeof e[t];
    l === "boolean" ? s = Un(s) : s == null && l === "string" ? (s = "", o = !0) : l === "number" && (s = 0, o = !0);
  }
  try {
    e[t] = s;
  } catch {
  }
  o && e.removeAttribute(r || t);
}
function Zo(e, t, s, n) {
  e.addEventListener(t, s, n);
}
function Qo(e, t, s, n) {
  e.removeEventListener(t, s, n);
}
const In = /* @__PURE__ */ Symbol("_vei");
function el(e, t, s, n, r = null) {
  const i = e[In] || (e[In] = {}), o = i[t];
  if (n && o)
    o.value = n;
  else {
    const [l, f] = tl(t);
    if (n) {
      const d = i[t] = rl(
        n,
        r
      );
      Zo(e, l, d, f);
    } else o && (Qo(e, l, o, f), i[t] = void 0);
  }
}
const Pn = /(?:Once|Passive|Capture)$/;
function tl(e) {
  let t;
  if (Pn.test(e)) {
    t = {};
    let n;
    for (; n = e.match(Pn); )
      e = e.slice(0, e.length - n[0].length), t[n[0].toLowerCase()] = !0;
  }
  return [e[2] === ":" ? e.slice(3) : Ze(e.slice(2)), t];
}
let ys = 0;
const sl = /* @__PURE__ */ Promise.resolve(), nl = () => ys || (sl.then(() => ys = 0), ys = Date.now());
function rl(e, t) {
  const s = (n) => {
    if (!n._vts)
      n._vts = Date.now();
    else if (n._vts <= s.attached)
      return;
    Ie(
      il(n, s.value),
      t,
      5,
      [n]
    );
  };
  return s.value = e, s.attached = nl(), s;
}
function il(e, t) {
  if (R(t)) {
    const s = e.stopImmediatePropagation;
    return e.stopImmediatePropagation = () => {
      s.call(e), e._stopped = !0;
    }, t.map(
      (n) => (r) => !r._stopped && n && n(r)
    );
  } else
    return t;
}
const Mn = (e) => e.charCodeAt(0) === 111 && e.charCodeAt(1) === 110 && // lowercase letter
e.charCodeAt(2) > 96 && e.charCodeAt(2) < 123, ol = (e, t, s, n, r, i) => {
  const o = r === "svg";
  t === "class" ? qo(e, n, o) : t === "style" ? zo(e, s, n) : Xt(t) ? Zt(t) || el(e, t, s, n, i) : (t[0] === "." ? (t = t.slice(1), !0) : t[0] === "^" ? (t = t.slice(1), !1) : ll(e, t, n, o)) ? (An(e, t, n), !e.tagName.includes("-") && (t === "value" || t === "checked" || t === "selected") && En(e, t, n, o, i, t !== "value")) : /* #11081 force set props for possible async custom element */ e._isVueCE && // #12408 check if it's declared prop or it's async custom element
  (cl(e, t) || // @ts-expect-error _def is private
  e._def.__asyncLoader && (/[A-Z]/.test(t) || !Y(n))) ? An(e, he(t), n, i, t) : (t === "true-value" ? e._trueValue = n : t === "false-value" && (e._falseValue = n), En(e, t, n, o));
};
function ll(e, t, s, n) {
  if (n)
    return !!(t === "innerHTML" || t === "textContent" || t in e && Mn(t) && j(s));
  if (t === "spellcheck" || t === "draggable" || t === "translate" || t === "autocorrect" || t === "sandbox" && e.tagName === "IFRAME" || t === "form" || t === "list" && e.tagName === "INPUT" || t === "type" && e.tagName === "TEXTAREA")
    return !1;
  if (t === "width" || t === "height") {
    const r = e.tagName;
    if (r === "IMG" || r === "VIDEO" || r === "CANVAS" || r === "SOURCE")
      return !1;
  }
  return Mn(t) && Y(s) ? !1 : t in e;
}
function cl(e, t) {
  const s = (
    // @ts-expect-error _def is private
    e._def.props
  );
  if (!s)
    return !1;
  const n = he(t);
  return Array.isArray(s) ? s.some((r) => he(r) === n) : Object.keys(s).some((r) => he(r) === n);
}
const fl = /* @__PURE__ */ Z({ patchProp: ol }, Bo);
let Rn;
function ul() {
  return Rn || (Rn = So(fl));
}
const al = (...e) => {
  const t = ul().createApp(...e), { mount: s } = t;
  return t.mount = (n) => {
    const r = hl(n);
    if (!r) return;
    const i = t._component;
    !j(i) && !i.render && !i.template && (i.template = r.innerHTML), r.nodeType === 1 && (r.textContent = "");
    const o = s(r, !1, dl(r));
    return r instanceof Element && (r.removeAttribute("v-cloak"), r.setAttribute("data-v-app", "")), o;
  }, t;
};
function dl(e) {
  if (e instanceof SVGElement)
    return "svg";
  if (typeof MathMLElement == "function" && e instanceof MathMLElement)
    return "mathml";
}
function hl(e) {
  return Y(e) ? document.querySelector(e) : e;
}
const pl = { class: "video-editor" }, gl = { class: "top-layout" }, _l = { class: "preview-panel" }, ml = { class: "video-stage" }, bl = ["src"], yl = { class: "overlay" }, vl = {
  class: "object-panel",
  "data-testid": "object-list"
}, xl = ["checked", "onChange"], Sl = {
  class: "timeline-panel",
  "data-testid": "timeline"
}, wl = { class: "timeline-grid" }, Cl = {
  class: "timeline-left",
  "data-testid": "timeline-object-list"
}, Tl = { class: "timeline-object-label" }, Ol = ["checked", "onChange"], El = { class: "timeline-right" }, Al = /* @__PURE__ */ Ui({
  __name: "VideoEditorApp",
  props: {
    state: {}
  },
  setup(e) {
    const t = e, s = /* @__PURE__ */ on(0), n = /* @__PURE__ */ on([]), r = [
      "#ef4444",
      "#22c55e",
      "#3b82f6",
      "#f59e0b",
      "#8b5cf6",
      "#ec4899",
      "#14b8a6",
      "#f97316"
    ];
    function i(v) {
      return v.trackId !== null && v.trackId !== void 0 ? `track-${v.trackId}` : `frameobj-${v.className ?? "object"}-${v.id}`;
    }
    function o(v) {
      const A = v.className ?? "Object";
      return v.trackId !== null && v.trackId !== void 0 ? `${A} #${v.trackId}` : A;
    }
    const l = _t(
      () => [...t.state.frames ?? []].sort((v, A) => v.timeSeconds - A.timeSeconds)
    ), f = _t(() => {
      const v = /* @__PURE__ */ new Map();
      for (const A of l.value)
        for (const F of A.detectedObjects ?? []) {
          const C = i(F);
          v.has(C) || v.set(C, {
            key: C,
            label: o(F),
            color: r[v.size % r.length],
            trackId: F.trackId
          });
        }
      return Array.from(v.values());
    });
    Kt(
      f,
      (v) => {
        const A = /* @__PURE__ */ new Set();
        for (const F of l.value)
          for (const C of F.detectedObjects ?? [])
            C.selected && A.add(i(C));
        n.value = A.size > 0 ? Array.from(A) : v.map((F) => F.key);
      },
      { immediate: !0 }
    );
    const d = _t(() => {
      if (l.value.length === 0)
        return null;
      let v = l.value[0];
      for (const A of l.value)
        if (A.timeSeconds <= s.value)
          v = A;
        else
          break;
      return v;
    }), a = _t(() => {
      const v = d.value;
      return v ? v.detectedObjects.filter((A) => n.value.includes(i(A))) : [];
    });
    function p(v, A) {
      if (A) {
        n.value.includes(v) || (n.value = [...n.value, v]);
        return;
      }
      n.value = n.value.filter((F) => F !== v);
    }
    function T(v) {
      return l.value.filter((A) => (A.detectedObjects ?? []).some((F) => i(F) === v)).map((A) => A.timeSeconds);
    }
    function O(v) {
      const A = v.target;
      s.value = A.currentTime;
    }
    function H(v) {
      var A;
      return ((A = f.value.find((F) => F.key === v)) == null ? void 0 : A.color) ?? "#3b82f6";
    }
    return (v, A) => {
      var F;
      return ae(), de("div", pl, [
        J("div", gl, [
          J("div", _l, [
            J("div", ml, [
              J("video", {
                src: e.state.videoSourceUrl,
                controls: "",
                onTimeupdate: O
              }, null, 40, bl),
              J("div", yl, [
                (ae(!0), de(le, null, ht(a.value, (C) => (ae(), de("div", {
                  key: C.id,
                  class: "bbox",
                  style: Ve({
                    left: `${C.x}px`,
                    top: `${C.y}px`,
                    width: `${C.width}px`,
                    height: `${C.height}px`,
                    borderColor: H(i(C))
                  })
                }, [
                  J("span", {
                    class: "bbox-label",
                    style: Ve({ backgroundColor: H(i(C)) })
                  }, Ut(o(C)), 5)
                ], 4))), 128))
              ])
            ])
          ]),
          J("aside", vl, [
            A[0] || (A[0] = J("h3", null, "Detected objects", -1)),
            (ae(!0), de(le, null, ht(((F = d.value) == null ? void 0 : F.detectedObjects) ?? [], (C) => (ae(), de("div", {
              key: C.id,
              class: "object-row"
            }, [
              J("label", null, [
                J("input", {
                  type: "checkbox",
                  checked: n.value.includes(i(C)),
                  onChange: (D) => p(i(C), D.target.checked)
                }, null, 40, xl),
                jr(" " + Ut(o(C)), 1)
              ])
            ]))), 128))
          ])
        ]),
        J("section", Sl, [
          J("div", wl, [
            J("div", Cl, [
              (ae(!0), de(le, null, ht(f.value, (C) => (ae(), de("div", {
                key: C.key,
                class: "timeline-object",
                "data-testid": "timeline-object",
                style: Ve({ borderLeftColor: C.color })
              }, [
                J("label", Tl, [
                  J("input", {
                    type: "checkbox",
                    checked: n.value.includes(C.key),
                    onChange: (D) => p(C.key, D.target.checked)
                  }, null, 40, Ol),
                  J("span", {
                    class: "timeline-color",
                    style: Ve({ backgroundColor: C.color })
                  }, null, 4),
                  J("span", null, Ut(C.label), 1)
                ])
              ], 4))), 128))
            ]),
            J("div", El, [
              (ae(!0), de(le, null, ht(f.value, (C) => (ae(), de("div", {
                key: C.key,
                class: "timeline-row"
              }, [
                (ae(!0), de(le, null, ht(T(C.key), (D) => (ae(), de("div", {
                  key: `${C.key}-${D}`,
                  class: "timeline-marker",
                  style: Ve({
                    left: `${Math.min(100, Math.max(0, D * 10))}%`,
                    backgroundColor: C.color
                  })
                }, null, 4))), 128))
              ]))), 128))
            ])
          ])
        ])
      ]);
    };
  }
}), Il = (e, t) => {
  const s = e.__vccOpts || e;
  for (const [n, r] of t)
    s[n] = r;
  return s;
}, Pl = /* @__PURE__ */ Il(Al, [["__scopeId", "data-v-1785ad15"]]);
window.mountVideoEditorVueApp = (e, t) => {
  const s = /* @__PURE__ */ ss({
    videoId: t.videoId,
    videoSourceUrl: t.videoSourceUrl,
    frames: t.frames ?? []
  }), n = al(Pl, { state: s });
  return n.mount(e), {
    update(r) {
      s.videoId = r.videoId, s.videoSourceUrl = r.videoSourceUrl, s.frames = r.frames ?? [];
    },
    unmount() {
      n.unmount();
    }
  };
};
