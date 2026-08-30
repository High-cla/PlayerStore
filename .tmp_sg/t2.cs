class T {
    void A() { try { } catch { } }
    void B() { try { } catch (System.Exception e) { } }
    void C() { try { } catch { Log(); } }
}
