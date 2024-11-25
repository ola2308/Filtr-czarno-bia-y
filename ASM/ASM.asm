.data
    align 16                        ; Wyr�wnanie do granicy 16 bajt�w dla optymalnego dost�pu SIMD
    rgb_weights REAL4 0.299, 0.587, 0.114, 0.0    ; Standardowe wsp�czynniki konwersji RGB->Grayscale
                                                  ; Ostatnia warto�� 0.0 to padding dla align 16
    
.code
GrayscaleFilter PROC
    ;==========================================
    ; Parametry wej�ciowe (konwencja MS x64):
    ; rcx - wska�nik do inputBuffer (�r�d�owy bufor RGB)
    ; rdx - wska�nik do outputBuffer (bufor wyj�ciowy)
    ; r8d - pixelCount (liczba pikseli do przetworzenia)
    ; xmm3 - brightness (wsp�czynnik jasno�ci)
    ;==========================================
    
    ; === Prolog funkcji i zachowanie rejestr�w ===
    push rbp                        ; Zachowanie base pointera
    mov rbp, rsp                   ; Ustanowienie nowej ramki stosu
    push rsi                       ; Zachowanie rejestr�w
    push rdi                       ; kt�re b�dziemy u�ywa�
    
    ; === Inicjalizacja rejestr�w g��wnych ===
    mov rsi, rcx                   ; rsi = wska�nik wej�ciowy
    mov rdi, rdx                   ; rdi = wska�nik wyj�ciowy
    mov r9d, r8d                   ; Kopia liczby pikseli (do obs�ugi pozosta�ych)
    mov ecx, r8d                   ; Licznik g��wnej p�tli
    
    ;=== Inicjalizacja rejestr�w wektorowych ===
    ; Przygotowanie wsp�czynnik�w RGB i jasno�ci
    movups xmm0, [rgb_weights]     ; Za�aduj wsp�czynniki RGB do xmm0 - SIMD pozwala na za�adowanie wszystkich wsp�czynnik�w jednocze�nie, co przyspiesza dalsze obliczenia (IW)
    shufps xmm3, xmm3, 0           ; Skopiuj warto�� jasno�ci do wszystkich element�w xmm3 - Replikacja jasno�ci na wszystkie elementy umo�liwia r�wnoleg�e przetwarzanie (IW)
    mulps xmm0, xmm3               ; Przemn� wsp�czynniki przez jasno�� - Optymalizuje operacj� przez jednoczesne przeliczenie wszystkich kana��w (IW)
    
    ; Przygotowanie masek dla ka�dego kana�u
    movaps xmm4, xmm0              ; Kopia wsp�czynnik�w do xmm4 (Blue) - Dublowanie wsp�czynnik�w, aby umo�liwi� osobne przetwarzanie ka�dego kana�u (IW)
    movaps xmm5, xmm0              ; Kopia wsp�czynnik�w do xmm5 (Green) (IW)
    movaps xmm6, xmm0              ; Kopia wsp�czynnik�w do xmm6 (Red) (IW)
    
    ; Rozpowszechnienie wsp�czynnik�w na wszystkie elementy
    shufps xmm4, xmm4, 0           ; Wszystkie elementy = wsp�czynnik Blue - Replikacja wsp�czynnika Blue dla wszystkich element�w wektora (IW)
    shufps xmm5, xmm5, 01010101b   ; Wszystkie elementy = wsp�czynnik Green - Replikacja wsp�czynnika Green dla wszystkich element�w wektora (IW)
    shufps xmm6, xmm6, 10101010b   ; Wszystkie elementy = wsp�czynnik Red - Replikacja wsp�czynnika Red dla wszystkich element�w wektora (IW)
    
    ; Przygotowanie rejestru zerowego do rozszerzania bajt�w
    pxor xmm7, xmm7                ; Wyzeruj xmm7 (b�dzie u�ywany jako maska) - Potrzebne do rozszerzania warto�ci pikseli do wi�kszych typ�w danych (IW)
    
    ; === Przygotowanie g��wnej p�tli ===
    shr ecx, 2                     ; Dzielimy liczb� pikseli przez 4 (przetwarzamy 4 naraz)
    test ecx, ecx                  ; Sprawd� czy s� piksele do przetworzenia
    jz process_remaining           ; Je�li nie, przejd� do przetwarzania pozosta�ych
    
process_pixels:
    ;=== Przetwarzanie 4 pikseli na raz ===
    ; �adowanie danych RGB (12 bajt�w = 4 piksele * 3 kolory)
    movd xmm1, dword ptr [rsi]     ; Za�aduj pierwsze 4 bajty
    movd xmm2, dword ptr [rsi+4]   ; Za�aduj kolejne 4 bajty
    movd xmm3, dword ptr [rsi+8]   ; Za�aduj ostatnie 4 bajty
    
    ; Rozszerzanie 8-bit -> 16-bit
    punpcklbw xmm1, xmm7           ; Rozszerz bajty Blue - Potrzebne do przetwarzania pikseli jako wi�kszych warto�ci (zwi�kszenie precyzji) (IW)
    punpcklbw xmm2, xmm7           ; Rozszerz bajty Green (IW)
    punpcklbw xmm3, xmm7           ; Rozszerz bajty Red (IW)
    
    ; Rozszerzanie 16-bit -> 32-bit
    punpcklwd xmm1, xmm7           ; Rozszerz s�owa Blue do dword�w - Umo�liwia dalsze przetwarzanie w formacie 32-bitowym (IW)
    punpcklwd xmm2, xmm7           ; Rozszerz s�owa Green do dword�w (IW)
    punpcklwd xmm3, xmm7           ; Rozszerz s�owa Red do dword�w (IW)
    
    ; Konwersja int -> float
    cvtdq2ps xmm1, xmm1            ; Konwertuj Blue na float - Umo�liwia mno�enie przez wsp�czynniki w formacie zmiennoprzecinkowym (IW)
    cvtdq2ps xmm2, xmm2            ; Konwertuj Green na float (IW)
    cvtdq2ps xmm3, xmm3            ; Konwertuj Red na float (IW)
    
    ; Mno�enie przez wsp�czynniki
    mulps xmm1, xmm4               ; Blue * wsp�czynnik - SIMD optymalizuje mno�enie wielu warto�ci naraz (IW)
    mulps xmm2, xmm5               ; Green * wsp�czynnik (IW)
    mulps xmm3, xmm6               ; Red * wsp�czynnik (IW)
    
    ; Sumowanie wszystkich kana��w
    addps xmm1, xmm2               ; Dodaj wyniki Green - SIMD pozwala na r�wnoczesne sumowanie wielu warto�ci (IW)
    addps xmm1, xmm3               ; Dodaj wyniki Red (IW)
    
    ; Konwersja wyniku z powrotem do int
    cvtps2dq xmm1, xmm1            ; float -> int - Konwersja wyniku na format ca�kowitoliczbowy przed zapisaniem (IW)
    
    ; Pakowanie wynik�w
    packssdw xmm1, xmm1            ; 32-bit -> 16-bit z saturacj� - Redukcja rozmiaru danych, uwzgl�dniaj�c ograniczenia zakresu (IW)
    packuswb xmm1, xmm1            ; 16-bit -> 8-bit z saturacj� unsigned - Przygotowanie danych do formatu wyj�ciowego (IW)
    
    ; Pobranie spakowanych wynik�w
    movd eax, xmm1                 ; Przenie� wyniki do rejestru og�lnego - Przygotowanie danych do zapisu
    
    ; Zapisanie wynik�w z replikacj� dla RGB (ka�dy piksel zapisywany 3 razy)
    mov byte ptr [rdi], al         ; Zapisz piksel 1 (B)
    mov byte ptr [rdi+1], al       ; Zapisz piksel 1 (G)
    mov byte ptr [rdi+2], al       ; Zapisz piksel 1 (R)
    shr eax, 8
    mov byte ptr [rdi+3], al       ; Zapisz piksel 2 (B)
    mov byte ptr [rdi+4], al       ; Zapisz piksel 2 (G)
    mov byte ptr [rdi+5], al       ; Zapisz piksel 2 (R)
    shr eax, 8
    mov byte ptr [rdi+6], al       ; Zapisz piksel 3 (B)
    mov byte ptr [rdi+7], al       ; Zapisz piksel 3 (G)
    mov byte ptr [rdi+8], al       ; Zapisz piksel 3 (R)
    shr eax, 8
    mov byte ptr [rdi+9], al       ; Zapisz piksel 4 (B)
    mov byte ptr [rdi+10], al      ; Zapisz piksel 4 (G)
    mov byte ptr [rdi+11], al      ; Zapisz piksel 4 (R)
    
    ; Przesuni�cie wska�nik�w
    add rsi, 12                    ; Przesu� wska�nik wej�ciowy (4 piksele * 3 bajty)
    add rdi, 12                    ; Przesu� wska�nik wyj�ciowy
    
    ; P�tla g��wna
    dec ecx                        ; Zmniejsz licznik
    jnz process_pixels             ; Kontynuuj je�li s� jeszcze piksele
    
process_remaining:
    ;=== Przetwarzanie pozosta�ych pikseli (0-3) ===
    and r9d, 3                     ; Oblicz pozosta�e piksele (reszta z dzielenia przez 4)
    jz cleanup                     ; Je�li nie ma pozosta�ych, zako�cz

remaining_loop:
    ; Przetwarzanie pojedynczego piksela
    movzx eax, byte ptr [rsi]      ; Za�aduj Blue
    cvtsi2ss xmm1, eax             ; Konwertuj na float
    mulss xmm1, xmm4               ; Pomn� przez wsp�czynnik Blue
    
    movzx eax, byte ptr [rsi+1]    ; Za�aduj Green
    cvtsi2ss xmm2, eax             ; Konwertuj na float
    mulss xmm2, xmm5               ; Pomn� przez wsp�czynnik Green
    addss xmm1, xmm2               ; Dodaj do sumy
    
    movzx eax, byte ptr [rsi+2]    ; Za�aduj Red
    cvtsi2ss xmm2, eax             ; Konwertuj na float
    mulss xmm2, xmm6               ; Pomn� przez wsp�czynnik Red
    addss xmm1, xmm2               ; Dodaj do sumy
    
    ; Konwersja wyniku do int
    cvtss2si eax, xmm1            
    
    ; Zapisz wynik jako RGB
    mov byte ptr [rdi], al         ; Zapisz jako Blue
    mov byte ptr [rdi+1], al       ; Zapisz jako Green
    mov byte ptr [rdi+2], al       ; Zapisz jako Red
    
    ; Przesu� wska�niki na nast�pny piksel
    add rsi, 3                     ; Nast�pny piksel wej�ciowy
    add rdi, 3                     ; Nast�pny piksel wyj�ciowy
    
    ; P�tla pozosta�ych pikseli
    dec r9d                        ; Zmniejsz licznik pozosta�ych
    jnz remaining_loop             ; Kontynuuj je�li s� jeszcze pozosta�e

cleanup:
    ;=== Epilog funkcji ===
    pop rdi                        ; Przywr�� zachowane rejestry
    pop rsi
    pop rbp                        ; Przywr�� base pointer
    ret                           ; Powr�t z funkcji

GrayscaleFilter ENDP
END
