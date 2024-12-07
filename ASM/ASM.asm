.data
    align 16                        ; Wyrównanie do 16 bajtów (standardowe dla SSE)
    rgb_weights REAL4 0.299, 0.587, 0.114, 0.0    ; Wspó³czynniki konwersji RGB->Grayscale
    rounding_const REAL4 0.5, 0.5, 0.5, 0.5       ; Sta³a do zaokr¹glania
    max_value REAL4 255.0, 255.0, 255.0, 255.0    ; Maksymalna wartoœæ piksela
    min_value REAL4 0.0, 0.0, 0.0, 0.0           ; Minimalna wartoœæ piksela

.code
GrayscaleFilter PROC
    ;==========================================
    ; Parametry wejœciowe (konwencja MS x64):
    ; rcx - wskaŸnik do inputBuffer
    ; rdx - wskaŸnik do outputBuffer
    ; r8d - pixelCount
    ; xmm3 - brightness
    ;==========================================
    
    ; === Prolog funkcji ===
    push rbp
    mov rbp, rsp
    push rsi
    push rdi
    push rbx
    
    ; === Inicjalizacja ===
    mov rsi, rcx                   ; Ÿród³owy bufor
    mov rdi, rdx                   ; bufor wyjœciowy
    mov ebx, r8d                   ; liczba pikseli
    
    ; === Inicjalizacja SIMD ===
    movups xmm0, [rgb_weights]     ; wspó³czynniki RGB
    shufps xmm3, xmm3, 0          ; rozproszenie jasnoœci
    mulps xmm0, xmm3              ; przemno¿enie wspó³czynników
    
    ; Przygotowanie wspó³czynników dla kana³ów
    movaps xmm4, xmm0             ; Blue
    movaps xmm5, xmm0             ; Green
    movaps xmm6, xmm0             ; Red
    
    shufps xmm4, xmm4, 0          ; replikacja Blue
    shufps xmm5, xmm5, 01010101b  ; replikacja Green
    shufps xmm6, xmm6, 10101010b  ; replikacja Red
    
    ; Za³adowanie sta³ych
    movups xmm7, [max_value]      ; 255.0
    movups xmm8, [min_value]      ; 0.0
    movups xmm9, [rounding_const] ; 0.5
    
    ; === G³ówna pêtla (4 piksele) ===
    mov ecx, ebx
    shr ecx, 2                    ; dzielenie przez 4
    test ecx, ecx
    jz process_remaining
    
process_pixels:
    ; £adowanie pikseli
    movd xmm1, dword ptr [rsi]    ; pierwsze 4 bajty
    movd xmm2, dword ptr [rsi+4]  ; kolejne 4 bajty
    movd xmm10, dword ptr [rsi+8] ; ostatnie 4 bajty
    
    ; Rozszerzenie bajtów
    pxor xmm15, xmm15             ; zerowy rejestr do unpacking
    punpcklbw xmm1, xmm15         ; rozszerzenie do word
    punpcklbw xmm2, xmm15
    punpcklbw xmm10, xmm15
    
    punpcklwd xmm1, xmm15         ; rozszerzenie do dword
    punpcklwd xmm2, xmm15
    punpcklwd xmm10, xmm15
    
    ; Konwersja int -> float
    cvtdq2ps xmm1, xmm1           ; Blue
    cvtdq2ps xmm2, xmm2           ; Green
    cvtdq2ps xmm10, xmm10         ; Red
    
    ; Obliczenie wartoœci szaroœci
    mulps xmm1, xmm4              ; mno¿enie przez wspó³czynniki
    mulps xmm2, xmm5
    mulps xmm10, xmm6
    
    addps xmm1, xmm2              ; sumowanie kana³ów
    addps xmm1, xmm10
    
    ; Zaokr¹glanie i ograniczenie zakresu
    addps xmm1, xmm9              ; dodanie 0.5
    maxps xmm1, xmm8              ; minimum 0
    minps xmm1, xmm7              ; maximum 255
    
    ; Konwersja z powrotem
    cvtps2dq xmm1, xmm1           ; float -> int
    packssdw xmm1, xmm1           ; int -> word
    packuswb xmm1, xmm1           ; word -> byte
    
    ; Zapis wyników
    movd eax, xmm1
    
    ; Zapis ka¿dego piksela jako RGB
    mov byte ptr [rdi], al        ; piksel 1
    mov byte ptr [rdi+1], al
    mov byte ptr [rdi+2], al
    shr eax, 8
    mov byte ptr [rdi+3], al      ; piksel 2
    mov byte ptr [rdi+4], al
    mov byte ptr [rdi+5], al
    shr eax, 8
    mov byte ptr [rdi+6], al      ; piksel 3
    mov byte ptr [rdi+7], al
    mov byte ptr [rdi+8], al
    shr eax, 8
    mov byte ptr [rdi+9], al      ; piksel 4
    mov byte ptr [rdi+10], al
    mov byte ptr [rdi+11], al
    
    ; Przesuniêcie wskaŸników
    add rsi, 12
    add rdi, 12
    
    dec ecx
    jnz process_pixels
    
process_remaining:
    ; === Pozosta³e piksele ===
    and ebx, 3                    ; reszta z dzielenia przez 4
    test ebx, ebx
    jz cleanup
    
remaining_loop:
    ; Przetwarzanie pojedynczego piksela
    movzx eax, byte ptr [rsi]     ; Blue
    cvtsi2ss xmm1, eax
    mulss xmm1, xmm4
    
    movzx eax, byte ptr [rsi+1]   ; Green
    cvtsi2ss xmm2, eax
    mulss xmm2, xmm5
    addss xmm1, xmm2
    
    movzx eax, byte ptr [rsi+2]   ; Red
    cvtsi2ss xmm2, eax
    mulss xmm2, xmm6
    addss xmm1, xmm2
    
    ; Zaokr¹glanie i zakres
    addss xmm1, xmm9
    maxss xmm1, xmm8
    minss xmm1, xmm7
    
    ; Konwersja i zapis
    cvtss2si eax, xmm1
    
    mov byte ptr [rdi], al
    mov byte ptr [rdi+1], al
    mov byte ptr [rdi+2], al
    
    add rsi, 3
    add rdi, 3
    
    dec ebx
    jnz remaining_loop
    
cleanup:
    pop rbx
    pop rdi
    pop rsi
    pop rbp
    ret

GrayscaleFilter ENDP
END