'use client';

import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Separator } from '@/components/ui/separator';
import { Button } from '@/components/ui/button';
import { completeCourseCheckout, type Product } from '@/lib/courses/actions/enrollment.actions';
import { getLearnerCourseContentHref } from '@/lib/learner/paths';
import { cn } from '@/lib/utils';
import { ArrowRight, CheckCircle2, CreditCard, Loader2, LockKeyhole, ShieldCheck } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useMemo, useState } from 'react';

interface CourseCheckoutButtonProps {
  readonly courseSlug: string;
  readonly products: Product[];
  readonly className?: string;
  readonly buttonClassName?: string;
}

function formatPrice(product: Product): string {
  if (product.price <= 0) return 'Free';

  try {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: product.currency || 'USD',
      maximumFractionDigits: product.price % 1 === 0 ? 0 : 2,
    }).format(product.price);
  } catch {
    return `${product.currency || 'USD'} ${product.price}`;
  }
}

export function CourseCheckoutButton({ courseSlug, products, className, buttonClassName }: CourseCheckoutButtonProps) {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isOpen, setIsOpen] = useState(false);
  const [selectedProductId, setSelectedProductId] = useState(products[0]?.id ?? '');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const selectedProduct = useMemo(
    () => products.find((product) => product.id === selectedProductId) ?? products[0],
    [products, selectedProductId],
  );

  const handleCheckout = async () => {
    if (!selectedProduct || isSubmitting) return;

    setIsSubmitting(true);
    setError(null);
    setSuccess(null);

    try {
      const result = await completeCourseCheckout(courseSlug, selectedProduct.id);

      if (!result.success) {
        setError(result.message);
        return;
      }

      setSuccess(result.message);
      router.push(result.learningUrl ?? getLearnerCourseContentHref(courseSlug));
    } catch (checkoutError) {
      setError(checkoutError instanceof Error ? checkoutError.message : 'Could not complete checkout.');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!selectedProduct) {
    return (
      <Button size="lg" disabled className={cn('bg-white/20 text-white', buttonClassName)}>
        Checkout unavailable
      </Button>
    );
  }

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <div className={cn('flex flex-col gap-3', className)}>
        <DialogTrigger asChild>
          <Button size="lg" className={cn('bg-white text-slate-950 hover:bg-slate-200', buttonClassName)}>
            Enroll for {formatPrice(selectedProduct)}
            <ArrowRight data-icon="inline-end" />
          </Button>
        </DialogTrigger>

        {success ? (
          <p className="flex items-center gap-2 text-xs text-emerald-300">
            <CheckCircle2 className="size-3.5" />
            {success}
          </p>
        ) : null}
      </div>

      <DialogContent className="max-w-2xl gap-0 overflow-hidden p-0">
        <DialogHeader className="border-b px-6 py-5">
          <DialogTitle>Complete enrollment</DialogTitle>
          <DialogDescription>
            Confirm your course access, payment summary, and where you will continue after checkout.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-5 px-6 py-5">
          <div className="rounded-lg border bg-muted/30 p-4">
            <div className="flex items-start justify-between gap-4">
              <div className="flex items-start gap-3">
                <div className="rounded-md border bg-background p-2">
                  <CreditCard className="size-4" />
                </div>
                <div className="flex flex-col gap-1">
                  <p className="font-medium">{selectedProduct.name}</p>
                  {selectedProduct.description ? (
                    <p className="max-w-md text-sm text-muted-foreground">{selectedProduct.description}</p>
                  ) : null}
                </div>
              </div>
              <p className="shrink-0 text-lg font-semibold">{formatPrice(selectedProduct)}</p>
            </div>
          </div>

          {products.length > 1 ? (
            <div className="flex flex-col gap-3">
              <p className="text-sm font-medium">Choose access option</p>
              <RadioGroup value={selectedProduct.id} onValueChange={setSelectedProductId} className="grid gap-3">
                {products.map((product) => (
                  <label
                    key={product.id}
                    className={cn(
                      'flex cursor-pointer items-start gap-3 rounded-lg border p-4 transition',
                      product.id === selectedProduct.id ? 'border-primary bg-primary/5' : 'hover:bg-muted/40',
                    )}
                  >
                    <RadioGroupItem value={product.id} className="mt-1" />
                    <span className="grid flex-1 gap-1">
                      <span className="flex items-center justify-between gap-4">
                        <span className="font-medium">{product.name}</span>
                        <span className="font-semibold">{formatPrice(product)}</span>
                      </span>
                      {product.description ? <span className="text-sm text-muted-foreground">{product.description}</span> : null}
                    </span>
                  </label>
                ))}
              </RadioGroup>
            </div>
          ) : null}

          <Separator />

          <div className="grid gap-3 text-sm">
            <div className="flex items-center justify-between gap-4">
              <span className="text-muted-foreground">Subtotal</span>
              <span>{formatPrice(selectedProduct)}</span>
            </div>
            <div className="flex items-center justify-between gap-4">
              <span className="text-muted-foreground">Taxes and fees</span>
              <span>Calculated by provider</span>
            </div>
            <div className="flex items-center justify-between gap-4 font-semibold">
              <span>Total due today</span>
              <span>{formatPrice(selectedProduct)}</span>
            </div>
          </div>

          <Alert>
            <ShieldCheck className="size-4" />
            <AlertTitle>Checkout confirmation</AlertTitle>
            <AlertDescription>
              Course access is activated only after the selected product is confirmed for your account.
            </AlertDescription>
          </Alert>

          {error ? (
            <Alert variant="destructive">
              <LockKeyhole className="size-4" />
              <AlertTitle>Checkout could not be completed</AlertTitle>
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          ) : null}
        </div>

        <DialogFooter className="border-t bg-muted/20 px-6 py-4">
          <Button variant="outline" onClick={() => setIsOpen(false)} disabled={isSubmitting}>
            Review course
          </Button>
          <Button onClick={() => void handleCheckout()} disabled={isSubmitting}>
            {isSubmitting ? (
              <>
                <Loader2 className="size-4 animate-spin" />
                Completing checkout...
              </>
            ) : (
              <>
                Confirm and enter classroom
                <ArrowRight data-icon="inline-end" />
              </>
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
