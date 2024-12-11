.data
    align 16                        ; wyrównanie do 16 bajtów
    ; we liczby s³u¿¹ do zmiany kolorowego obrazka na czarno-bia³y
    rgb_weights REAL4 0.299, 0.587, 0.114, 0.0   
    rounding_const REAL4 0.5, 0.5, 0.5, 0.5       ; liczby do zaokr¹glania, ¿eby nie by³o brzydkich wartoœci
    max_value REAL4 255.0, 255.0, 255.0, 255.0    ; najwiêksza jasnoœæ jak¹ mo¿e mieæ piksel
    min_value REAL4 0.0, 0.0, 0.0, 0.0           ; najmniejsza jasnoœæ jak¹ mo¿e mieæ piksel

.code
GrayscaleFilter PROC
    ;==========================================
    ; Parametry wejœciowe (konwencja MS x64):
    ; rcx - wskaŸnik gdzie jest obraz
    ; rdx - wskaŸnik gdzie zapisaæ obraz
    ; r8d - pixelCount
    ; xmm3 - jasnoœæ
    ;==========================================

    ;robimy porz¹dek w pamiêci
    push rbp ; zapisujemy gdzie teraz jesteœmy, ¿eby póŸniej umieæ wróciæ
    mov rbp, rsp ; tu bêd¹ nasze nowe rzeczy
    push rsi  ; zapisujemy gdzie mamy obrazek Ÿród³owy
    push rdi ; zapisujemy gdzie bêdziemy dawaæ nowy obrazek
    push rbx ; zapisujemy jeszcze jedno miejsce na ró¿ne rzeczy
    
    mov rsi, rcx  ; tu jest nasz kolorowy obrazek
    mov rdi, rdx  ; tu bêdziemy robiæ czarno-bia³y obrazek
    mov ebx, r8d ; tu mamy zapisane ile pikseli musimy zmieniæ
    
    ;przygotowujemy super liczby do zmiany kolorów
    movups xmm0, [rgb_weights] ; bierzemy nasze liczby do zmiany kolorów 
    shufps xmm3, xmm3, 0  ; kopiujemy wartoœæ jasnoœci wszêdzie
    mulps xmm0, xmm3  ; mno¿ymy nasze liczby przez jasnoœæ
    
    ;robimy kopie naszych liczb bo bêdziemy ich du¿o u¿ywaæ
    movaps xmm4, xmm0     ; kopia dla czerwonego koloru       
    movaps xmm5, xmm0     ; kopia dla zielonego koloru     
    movaps xmm6, xmm0     ; kopia dla niebieskiego koloru     
    
    
    shufps xmm4, xmm4, 0          ; kopiujemy czerwony wszêdzie
    shufps xmm5, xmm5, 01010101b  ; kopiujemy zielony wszêdzie
    shufps xmm6, xmm6, 10101010b  ; kopiujemy niebieski wszêdzie
    
    movups xmm7, [max_value]     ; najbardziej bia³y kolor jaki mo¿e byæ
    movups xmm8, [min_value]     ; najbardziej czarny kolor
    movups xmm9, [rounding_const] ; liczby do zaokr¹glania
    
    ;ile grup po 4 piksele musimy zrobiæ
    mov ecx, ebx    ; bierzemy liczbê wszystkich pikseli
    shr ecx, 2      ; dzielimy przez 4 bo bêdziemy robiæ po 4 na raz, zwi¹zane z SHR(Shift Right) - przesuniêcie bitowe w prawo. przesuwamy bit o 2 pozycje w prawo czyli dzielimy przez 4           
    test ecx, ecx   ; sprawdzamy czy w ogóle jest co robiæ
    jz process_remaining  ; jak nie ma co robiæ po 4, to robimy pojedynczo

    
process_pixels:
    ; tu zaczyna siê g³ówna robota - robimy 4 piksele na raz
    movd xmm1, dword ptr [rsi]    ; bierzemy pierwszy piksel
    movd xmm2, dword ptr [rsi+4]  ; bierzemy drugi piksel
    movd xmm10, dword ptr [rsi+8] ; bierzemy trzeci piksel
    
    ; zamieniamy ma³e liczby na wiêksze bo tak ³atwiej liczyæ (rozszerzamy do 16 bajtów)
    pxor xmm15, xmm15    ; robimy sobie zero do pomocy        
    punpcklbw xmm1, xmm15     ; robimy z malutkich liczb wiêksze    
    punpcklbw xmm2, xmm15   ; to samo dla drugiego piksela
    punpcklbw xmm10, xmm15  ; i dla trzeciego

    ;robimy jeszcze wiêksze liczby(rozszerzamy do 32 bajtów)
    punpcklwd xmm1, xmm15         
    punpcklwd xmm2, xmm15
    punpcklwd xmm10, xmm15
    
    ; Konwersja int -> float
    cvtdq2ps xmm1, xmm1     ; teraz bêd¹ u³amki     
    cvtdq2ps xmm2, xmm2      ; bo na u³amkach lepiej siê liczy   
    cvtdq2ps xmm10, xmm10      ; dla wszystkich pikseli
    
    ; Liczymy jak szary ma byæ ka¿dy piksel
    mulps xmm1, xmm4   ; mno¿ymy przez nasze liczby           
    mulps xmm2, xmm5  
    mulps xmm10, xmm6   
    
    ; sk³adamy wszystko do kupy
    addps xmm1, xmm2   ; dodajemy wszystkie kolory razem          
    addps xmm1, xmm10   ; ¿eby wyszed³ jeden szary kolor
    
    ; Zaokr¹glanie i ograniczenie zakresu
    addps xmm1, xmm9        ; dodajemy nasze liczby do zaokr¹glania     
    maxps xmm1, xmm8         ; pilnujemy ¿eby nie by³o za ciemno     
    minps xmm1, xmm7         ; pilnujemy ¿eby nie by³o za jasno     
    
    ; Zamieniamy z powrotem na liczby ca³kowite
    cvtps2dq xmm1, xmm1           ; float -> int
    packssdw xmm1, xmm1           ; int -> word
    packuswb xmm1, xmm1           ; word -> byte
    
    ; Zapisujemy nasz szary kolor
    movd eax, xmm1
    
    ; Zapisujemy ka¿dy piksel jako szary (trzy razy ten sam kolor)
    mov byte ptr [rdi], al        ; piksel 1
    mov byte ptr [rdi+1], al     ; trzy razy to samo bo RGB
    mov byte ptr [rdi+2], al    ; ale wszystkie takie same = szary
    shr eax, 8                   ; bierzemy nastêpny piksel
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
    
    ;przesuwamy siê na nastêpne piksele
    add rsi, 12 ; przesuwamy siê w obrazku Ÿród³owym
    add rdi, 12 ; przesuwamy siê w nowym obrazku
    
    dec ecx ; zmniejszamy licznik pikseli
    jnz process_pixels ; jeœli jeszcze s¹ piksele to robimy dalej
    
process_remaining:
    ;Pozosta³e piksele
    and ebx, 3                    ; reszta z dzielenia przez 4
    test ebx, ebx
    jz cleanup
    
remaining_loop:
    ; zajmujemy siê pojedynczymi pikselami które zosta³y
    movzx eax, byte ptr [rsi]   ; bierzemy czerwony (R) z piksela  
    cvtsi2ss xmm1, eax          ; zamieniamy na liczbê z przecinkiem
    mulss xmm1, xmm4            ; mno¿ymy przez nasz¹ magiczn¹ liczbê dla czerwonego
    
    movzx eax, byte ptr [rsi+1]   
    cvtsi2ss xmm2, eax
    mulss xmm2, xmm5
    addss xmm1, xmm2
    
    movzx eax, byte ptr [rsi+2]   
    cvtsi2ss xmm2, eax
    mulss xmm2, xmm6
    addss xmm1, xmm2
    
    ; poprawiamy nasz¹ liczbê ¿eby by³a ³adna
    addss xmm1, xmm9    ; dodajemy liczbê do zaokr¹glania
    maxss xmm1, xmm8    ; pilnujemy ¿eby nie by³o za ciemno
    minss xmm1, xmm7    ; pilnujemy ¿eby nie by³o za jasno
    
    ;zamieniamy na zwyk³¹ liczbê i zapisujemy
    cvtss2si eax, xmm1

    ; zapisujemy nasz szary kolor trzy razy (bo RGB)
    mov byte ptr [rdi], al
    mov byte ptr [rdi+1], al
    mov byte ptr [rdi+2], al
    
    ;przesuwamy siê na nastêpny piksel
    add rsi, 3 ; przesuwamy siê w starym obrazku (3 bo RGB)
    add rdi, 3  ; przesuwamy siê w nowym obrazku (3 bo RGB)
   
   ;sprawdzamy czy to ju¿ wszystko
    dec ebx ; zmniejszamy licznik pikseli
    jnz remaining_loop  ; jeœli zosta³y jeszcze jakieœ, robimy dalej

;koniec programu - sprz¹tamy po sobie  
cleanup:
    pop rbx ; zbieramy nasze rzeczy które od³o¿yliœmy
    pop rdi ; ka¿d¹ po kolei
    pop rsi ; tak jak uk³adaliœmy
    pop rbp ; na samym pocz¹tku
    ret ; koñczymy program

GrayscaleFilter ENDP
END