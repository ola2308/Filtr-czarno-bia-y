.data
    align 16                        ; Wyrównanie do granicy 16 bajtów dla optymalnego dostêpu SIMD
    rgb_weights REAL4 0.299, 0.587, 0.114, 0.0    ; Standardowe wspó³czynniki konwersji RGB->Grayscale
                                                  ; Ostatnia wartoœæ 0.0 to padding dla align 16
    
.code
GrayscaleFilter PROC
    ;==========================================
    ; Parametry wejœciowe (konwencja MS x64):
    ; rcx - wskaŸnik do inputBuffer (Ÿród³owy bufor RGB)
    ; rdx - wskaŸnik do outputBuffer (bufor wyjœciowy)
    ; r8d - pixelCount (liczba pikseli do przetworzenia)
    ; xmm3 - brightness (wspó³czynnik jasnoœci)
    ;==========================================
    
    ; === Prolog funkcji i zachowanie rejestrów ===
    push rbp                        ; Zachowanie base pointera
    mov rbp, rsp                   ; Ustanowienie nowej ramki stosu
    push rsi                       ; Zachowanie rejestrów
    push rdi                       ; które bêdziemy u¿ywaæ
    
    ; === Inicjalizacja rejestrów g³ównych ===
    mov rsi, rcx                   ; rsi = wskaŸnik wejœciowy
    mov rdi, rdx                   ; rdi = wskaŸnik wyjœciowy
    mov r9d, r8d                   ; Kopia liczby pikseli (do obs³ugi pozosta³ych)
    mov ecx, r8d                   ; Licznik g³ównej pêtli
    
    ;=== Inicjalizacja rejestrów wektorowych ===
    ; Przygotowanie wspó³czynników RGB i jasnoœci
    movups xmm0, [rgb_weights]     ; Za³aduj wspó³czynniki RGB do xmm0
    shufps xmm3, xmm3, 0          ; Skopiuj wartoœæ jasnoœci do wszystkich elementów xmm3
    mulps xmm0, xmm3              ; Przemnó¿ wspó³czynniki przez jasnoœæ
    
    ; Przygotowanie masek dla ka¿dego kana³u
    movaps xmm4, xmm0             ; Kopia wspó³czynników do xmm4 (Blue)
    movaps xmm5, xmm0             ; Kopia wspó³czynników do xmm5 (Green)
    movaps xmm6, xmm0             ; Kopia wspó³czynników do xmm6 (Red)
    
    ; Rozpowszechnienie wspó³czynników na wszystkie elementy
    shufps xmm4, xmm4, 0          ; Wszystkie elementy = wspó³czynnik Blue
    shufps xmm5, xmm5, 01010101b  ; Wszystkie elementy = wspó³czynnik Green
    shufps xmm6, xmm6, 10101010b  ; Wszystkie elementy = wspó³czynnik Red
    
    ; Przygotowanie rejestru zerowego do rozszerzania bajtów
    pxor xmm7, xmm7               ; Wyzeruj xmm7 (bêdzie u¿ywany jako maska)
    
    ; === Przygotowanie g³ównej pêtli ===
    shr ecx, 2                    ; Dzielimy liczbê pikseli przez 4 (przetwarzamy 4 naraz)
    test ecx, ecx                 ; SprawdŸ czy s¹ piksele do przetworzenia
    jz process_remaining          ; Jeœli nie, przejdŸ do przetwarzania pozosta³ych
    
process_pixels:
    ;=== Przetwarzanie 4 pikseli na raz ===
    ; £adowanie danych RGB (12 bajtów = 4 piksele * 3 kolory)
    movd xmm1, dword ptr [rsi]    ; Za³aduj pierwsze 4 bajty
    movd xmm2, dword ptr [rsi+4]  ; Za³aduj kolejne 4 bajty
    movd xmm3, dword ptr [rsi+8]  ; Za³aduj ostatnie 4 bajty
    
    ; Rozszerzanie 8-bit -> 16-bit
    punpcklbw xmm1, xmm7          ; Rozszerz bajty Blue
    punpcklbw xmm2, xmm7          ; Rozszerz bajty Green
    punpcklbw xmm3, xmm7          ; Rozszerz bajty Red
    
    ; Rozszerzanie 16-bit -> 32-bit
    punpcklwd xmm1, xmm7          ; Rozszerz s³owa Blue do dwordów
    punpcklwd xmm2, xmm7          ; Rozszerz s³owa Green do dwordów
    punpcklwd xmm3, xmm7          ; Rozszerz s³owa Red do dwordów
    
    ; Konwersja int -> float
    cvtdq2ps xmm1, xmm1           ; Konwertuj Blue na float
    cvtdq2ps xmm2, xmm2           ; Konwertuj Green na float
    cvtdq2ps xmm3, xmm3           ; Konwertuj Red na float
    
    ; Mno¿enie przez wspó³czynniki
    mulps xmm1, xmm4              ; Blue * wspó³czynnik
    mulps xmm2, xmm5              ; Green * wspó³czynnik
    mulps xmm3, xmm6              ; Red * wspó³czynnik
    
    ; Sumowanie wszystkich kana³ów
    addps xmm1, xmm2              ; Dodaj wyniki Green
    addps xmm1, xmm3              ; Dodaj wyniki Red
    
    ; Konwersja wyniku z powrotem do int
    cvtps2dq xmm1, xmm1           ; float -> int
    
    ; Pakowanie wyników
    packssdw xmm1, xmm1           ; 32-bit -> 16-bit z saturacj¹
    packuswb xmm1, xmm1           ; 16-bit -> 8-bit z saturacj¹ unsigned
    
    ; Pobranie spakowanych wyników
    movd eax, xmm1                ; Przenieœ wyniki do rejestru ogólnego
    
    ; Zapisanie wyników z replikacj¹ dla RGB (ka¿dy piksel zapisywany 3 razy)
    mov byte ptr [rdi], al        ; Zapisz piksel 1 (B)
    mov byte ptr [rdi+1], al      ; Zapisz piksel 1 (G)
    mov byte ptr [rdi+2], al      ; Zapisz piksel 1 (R)
    shr eax, 8
    mov byte ptr [rdi+3], al      ; Zapisz piksel 2 (B)
    mov byte ptr [rdi+4], al      ; Zapisz piksel 2 (G)
    mov byte ptr [rdi+5], al      ; Zapisz piksel 2 (R)
    shr eax, 8
    mov byte ptr [rdi+6], al      ; Zapisz piksel 3 (B)
    mov byte ptr [rdi+7], al      ; Zapisz piksel 3 (G)
    mov byte ptr [rdi+8], al      ; Zapisz piksel 3 (R)
    shr eax, 8
    mov byte ptr [rdi+9], al      ; Zapisz piksel 4 (B)
    mov byte ptr [rdi+10], al     ; Zapisz piksel 4 (G)
    mov byte ptr [rdi+11], al     ; Zapisz piksel 4 (R)
    
    ; Przesuniêcie wskaŸników
    add rsi, 12                   ; Przesuñ wskaŸnik wejœciowy (4 piksele * 3 bajty)
    add rdi, 12                   ; Przesuñ wskaŸnik wyjœciowy
    
    ; Pêtla g³ówna
    dec ecx                       ; Zmniejsz licznik
    jnz process_pixels            ; Kontynuuj jeœli s¹ jeszcze piksele
    
process_remaining:
    ;=== Przetwarzanie pozosta³ych pikseli (0-3) ===
    and r9d, 3                    ; Oblicz pozosta³e piksele (reszta z dzielenia przez 4)
    jz cleanup                    ; Jeœli nie ma pozosta³ych, zakoñcz

remaining_loop:
    ; Przetwarzanie pojedynczego piksela
    movzx eax, byte ptr [rsi]     ; Za³aduj Blue
    cvtsi2ss xmm1, eax            ; Konwertuj na float
    mulss xmm1, xmm4              ; Pomnó¿ przez wspó³czynnik Blue
    
    movzx eax, byte ptr [rsi+1]   ; Za³aduj Green
    cvtsi2ss xmm2, eax            ; Konwertuj na float
    mulss xmm2, xmm5              ; Pomnó¿ przez wspó³czynnik Green
    addss xmm1, xmm2              ; Dodaj do sumy
    
    movzx eax, byte ptr [rsi+2]   ; Za³aduj Red
    cvtsi2ss xmm2, eax            ; Konwertuj na float
    mulss xmm2, xmm6              ; Pomnó¿ przez wspó³czynnik Red
    addss xmm1, xmm2              ; Dodaj do sumy
    
    ; Konwersja wyniku do int
    cvtss2si eax, xmm1            
    
    ; Zapisz wynik jako RGB
    mov byte ptr [rdi], al        ; Zapisz jako Blue
    mov byte ptr [rdi+1], al      ; Zapisz jako Green
    mov byte ptr [rdi+2], al      ; Zapisz jako Red
    
    ; Przesuñ wskaŸniki na nastêpny piksel
    add rsi, 3                    ; Nastêpny piksel wejœciowy
    add rdi, 3                    ; Nastêpny piksel wyjœciowy
    
    ; Pêtla pozosta³ych pikseli
    dec r9d                       ; Zmniejsz licznik pozosta³ych
    jnz remaining_loop            ; Kontynuuj jeœli s¹ jeszcze pozosta³e

cleanup:
    ;=== Epilog funkcji ===
    pop rdi                       ; Przywróæ zachowane rejestry
    pop rsi
    pop rbp                       ; Przywróæ base pointer
    ret                          ; Powrót z funkcji

GrayscaleFilter ENDP
END